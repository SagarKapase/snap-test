// APIBee on Azure Container Apps: one always-on replica behind HTTPS.
//
//   az group create -n rg-apibee -l centralindia
//   az deployment group create -g rg-apibee -f infra/main.bicep -p image=ghcr.io/sagarkapase/apibee:1.0.1
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

@secure()
@description('JWT signing key (32+ characters). A random key is generated on every deployment when omitted.')
param jwtKey string = '${newGuid()}${newGuid()}'

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${name}-logs'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    workspaceCapping: { dailyQuotaGb: 1 } // hard cap so logging can never run up a surprise bill
  }
}

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
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
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'      // HTTP/1.1 + HTTP/2; WebSockets and server-sent events work
        allowInsecure: false   // plain HTTP is redirected to HTTPS
      }
      secrets: [
        { name: 'jwt-key', value: jwtKey }
      ]
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
          env: [
            // Trust X-Forwarded-For / X-Forwarded-Proto from the Container Apps ingress: real client IPs in
            // /api/echo and correct https URLs (e.g. the Swagger OAuth redirect).
            { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
            { name: 'Jwt__Key', secretRef: 'jwt-key' }
          ]
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
