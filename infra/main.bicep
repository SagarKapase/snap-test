// TestingAPIs on Azure Container Apps: one always-on replica behind HTTPS.
//
//   az group create -n rg-apibee -l centralindia
//   az deployment group create -g rg-apibee -f infra/main.bicep -p image=ghcr.io/sagarkapase/apibee:1.0.1
//
// To add the app to a Container Apps environment you already have (e.g. when the subscription allows only one
// environment per region), pass its resource ID:  -p existingEnvironmentId=<environment resource id>
//
// Production (testingapis.com) settings live in infra/main.parameters.json. Redeploy with
//   az deployment group create -g rg-apibee -f infra/main.bicep -p @infra/main.parameters.json
// Deploying WITHOUT that file leaves customDomains empty and detaches the custom domains from the app.
// The proxy key is secret, so it is NOT in that file: add  -p proxyAccessKey=<key>  (omitting it disables the proxy).
//
// The app keeps all data in memory, so it must run exactly ONE replica: with two, a record created on one
// replica would be missing on the other. minReplicas = maxReplicas = 1 also means it never sleeps (no cold starts).

@description('Azure region. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Base name for all resources; also the container app name.')
param name string = 'apibee'

@description('Container image to run.')
param image string = 'ghcr.io/sagarkapase/apibee:latest'

@description('vCPU per replica (0.25, 0.5, 0.75, 1.0, ...). Memory must be 2 GiB per vCPU.')
param cpu string = '0.25'

@description('Memory per replica, matching cpu (0.5Gi for 0.25 vCPU, 1Gi for 0.5 vCPU, ...).')
param memory string = '0.5Gi'

@description('Resource ID of an existing Container Apps environment to deploy into. Leave empty to create a new one.')
param existingEnvironmentId string = ''

var createEnvironment = empty(existingEnvironmentId)

@description('Custom domains bound to managed certificates in the environment: [{ name, certificateName }].')
param customDomains array = []

@secure()
@description('Access key for /api/Proxy/call (sent by clients in the X-Proxy-Key header). Empty keeps the proxy disabled.')
param proxyAccessKey string = ''

var appSecrets = concat([{ name: 'jwt-key', value: jwtKey }], empty(proxyAccessKey) ? [] : [{ name: 'proxy-key', value: proxyAccessKey }])
var proxyEnv = empty(proxyAccessKey) ? [] : [{ name: 'Proxy__AccessKey', secretRef: 'proxy-key' }]

@secure()
@description('JWT signing key (32+ characters). A random key is generated on every deployment when omitted.')
param jwtKey string = '${newGuid()}${newGuid()}'

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = if (createEnvironment) {
  name: '${name}-logs'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    workspaceCapping: { dailyQuotaGb: 1 } // hard cap so logging can never run up a surprise bill
  }
}

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = if (createEnvironment) {
  name: '${name}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  properties: {
    managedEnvironmentId: createEnvironment ? env.id : existingEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'      // HTTP/1.1 + HTTP/2; WebSockets and server-sent events work
        allowInsecure: false   // plain HTTP is redirected to HTTPS
        customDomains: [for d in customDomains: {
          name: d.name
          certificateId: '${createEnvironment ? env.id : existingEnvironmentId}/managedCertificates/${d.certificateName}'
          bindingType: 'SniEnabled'
        }]
      }
      secrets: appSecrets
    }
    template: {
      containers: [
        {
          name: name
          image: image
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: concat([
            // Trust X-Forwarded-For / X-Forwarded-Proto from the Container Apps ingress: real client IPs in
            // /api/echo and correct https URLs (e.g. the Swagger OAuth redirect).
            { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
            { name: 'Jwt__Key', secretRef: 'jwt-key' }
          ], proxyEnv)
          probes: [
            { type: 'Startup', httpGet: { path: '/api/health/live', port: 8080 }, initialDelaySeconds: 2, periodSeconds: 3, failureThreshold: 20 }
            { type: 'Liveness', httpGet: { path: '/api/health/live', port: 8080 }, periodSeconds: 30 }
            { type: 'Readiness', httpGet: { path: '/api/health/ready', port: 8080 }, periodSeconds: 10 }
          ]
        }
      ]
      scale: {
        minReplicas: 1 // always on: never scales to zero, so no cold starts
        maxReplicas: 1 // in-memory data must live in a single replica
      }
    }
  }
}

output url string = 'https://${app.properties.configuration.ingress.fqdn}'
output containerAppName string = app.name
