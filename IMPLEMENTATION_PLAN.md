# APIBee Implementation Plan

Adapts the **APIBee PRD** (artifact `ed064582`) to the existing `snap-test` ASP.NET Core (.NET 9) project, matching current conventions.

- **Author:** Sagar K. | **Adapted:** 18 Jul 2026 | **Scope:** Backend API only
- **Data model:** In-memory `static List<T>` seed stores (like `FormDataStore` / `UserConstants`). Writes mutate the static list and **reset on app restart** — non-persistent, exactly per PRD.

## Conventions to follow (from existing code)

- One controller per resource: `[ApiController]` + `[Route("api/[controller]")]`.
- POCO models in `snap_test.Models`; static seed stores in a `Data/` folder (pattern of `FormDataStore`).
- Every action wrapped in try/catch → `StatusCode(500, new { message, error = ex.Message })`.
- Standard results: `Ok(...)`, `NotFound(new { message })`, `StatusCode(201, new { message, data })`, `Ok(new { message, data })` for updates, `Ok(new { message })` for deletes.
- `[Route("api/[controller]")]` naturally yields `/api/products`, `/api/posts`, etc. — matches PRD paths. Nested/custom paths use explicit `[HttpGet("...")]` route templates.

---

## Phase 0 — Shared Infrastructure (prerequisite, NOT in PRD) — ✅ DONE

Products (PRD Phase 1) depends on the global query features, so build these first.

### 0.1 `QueryHelper` static class (`Helpers/QueryHelper.cs`)
Reusable pipeline every list endpoint calls. Applies, in order:
1. **Field filters** — reflect over query keys matching model property names, filter by exact (case-insensitive) value. Covers `?category=`, `?completed=true`, `?userId=101`, `?status=shipped`, etc.
2. **Search `?q=`** — case-insensitive `Contains` across string fields named `title`, `name`, `body`, `description`, `text`.
3. **Sort `?sort=` + `?order=`** — order by named property; `desc` reverses. Default `asc`.
4. **Pagination `?limit=` / `?page=` / `?offset=`** — `limit` max 100; `page` is 1-based and requires `limit`; `offset` skips N. Return total count for headers.
5. Emits pagination values so the controller can set `X-Total-Count`, `X-Page`, `X-Per-Page`, `X-Total-Pages`.

Signature sketch:
```csharp
public static (List<T> items, int total, int page, int perPage, int totalPages)
    Apply<T>(IEnumerable<T> source, IQueryCollection query)
```
Controllers call it then `Response.Headers.Append(...)` for the pagination headers. Reflection-based so it works generically across all 11 resources — write once.

### 0.2 `SimulationMiddleware` (`Middleware/SimulationMiddleware.cs`) — PRD Phase 5, built early
Cross-cutting `?delay=` / `?error=` — must run before controllers, so implement as middleware now and register in `Program.cs`.
- `?delay=N` → `await Task.Delay(Math.Min(N,10)*1000)` before `next()`.
- `?error=CODE` (400,401,403,404,408,429,500,502,503) → short-circuit with the standard error body `{ status, error, message, simulated:true }` and header `X-Simulated: true`.
- `?delay=2&error=503` → delay first, then error.

### 0.3 CORS + standard headers (`Program.cs`)
- Add a permissive CORS policy: `AllowAnyOrigin/Method/Header`, expose `X-Total-Count, X-Page, X-Per-Page, X-Total-Pages, X-Simulated`.
- Middleware to append `X-Powered-By: APIBee` and the informational `X-RateLimit-*` headers (no actual limiting).
- Register `app.UseCors(...)` and the simulation middleware in the pipeline (before `MapControllers`).

### 0.4 Shared response helpers (optional `Helpers/ApiResponse.cs`)
Small helpers for the standard error body `{ status, error, message }` to keep controllers DRY.

**Deliverables:** `QueryHelper.cs`, `SimulationMiddleware.cs`, `Program.cs` edits, `ApiResponse.cs`. No new endpoints yet — verified via existing endpoints (`/api/user/getAllUsers?delay=2`, `?error=500`).

---

## Phase 1 — Products (PRD §3) — ✅ DONE

- **Model** `Models/Product.cs` — id, title, price, description, category, image, `Rating { rate, count }`, inStock, createdAt.
- **Store** `Data/ProductStore.cs` — 20 seed products: Electronics(5), Clothing(4), Books(4), Home(4), Sports(3).
- **Controller** `Controllers/ProductsController.cs`:
  - `GET /api/products` (via QueryHelper: pagination/sort/filter/search)
  - `GET /api/products/{id}`
  - `GET /api/products/categories` → distinct category strings
  - `GET /api/products/category/{name}`
  - `POST /api/products` (201, generated id)
  - `PUT /api/products/{id}`
  - `DELETE /api/products/{id}`

**Milestone:** e-commerce browse works end-to-end with all query features. Ship, then continue.

---

## Phase 2 — Posts, Comments, Todos (PRD §4) — ✅ DONE

- **Posts** `Models/Post.cs` (id, userId, title, body, tags[], publishedAt, likes) + `Data/PostStore.cs` (15 seed).
  - `GET /api/posts`, `GET /api/posts/{id}`, `GET /api/posts/{id}/comments` (nested), `GET /api/posts/user/{userId}`, POST/PUT/DELETE.
- **Comments** `Models/Comment.cs` (id, postId, userId, body, createdAt) + `Data/CommentStore.cs` (50 seed across 15 posts).
  - `GET /api/comments` (filter `?postId=`), `GET /api/comments/{id}`, POST, DELETE.
- **Todos** `Models/Todo.cs` (id, userId, title, completed, priority, dueDate) + `Data/TodoStore.cs` (30 seed).
  - `GET /api/todos` (filters `?completed=`, `?priority=`, `?userId=`), `GET /api/todos/{id}`, POST/PUT/DELETE.

Relationships reference existing User ids (101–110).

---

## Phase 3 — Carts & Orders (PRD §5) — ✅ DONE

- **Carts** `Models/Cart.cs` + `Models/CartItem.cs` (productId, quantity, price); `Data/CartStore.cs` (8 seed, 1–4 items).
  - `GET /api/carts` (`?userId=`), `GET /api/carts/{id}`, POST/PUT/DELETE.
- **Orders** `Models/Order.cs` + `Models/OrderItem.cs` + `Models/ShippingAddress.cs`; `Data/OrderStore.cs` (12 seed; statuses pending/processing/shipped/delivered/cancelled).
  - `GET /api/orders` (`?userId=`, `?status=`), `GET /api/orders/{id}`, POST, PUT (status change).

Items reference real Product ids from Phase 1.

---

## Phase 4 — Quotes & Recipes (PRD §6) — ✅ DONE

- **Quotes** `Models/Quote.cs` (id, text, author, category); `Data/QuoteStore.cs` (50 seed: programming/motivation/wisdom/humor/design).
  - `GET /api/quotes` (`?category=`), `GET /api/quotes/{id}`, `GET /api/quotes/random`.
- **Recipes** `Models/Recipe.cs` (id, title, cuisine, prepTime, cookTime, servings, difficulty, ingredients[], instructions[], image, rating, tags[]); `Data/RecipeStore.cs` (15 seed: Italian/Thai/Japanese/Mexican/Indian/American).
  - `GET /api/recipes` (`?cuisine=`, `?difficulty=`), `GET /api/recipes/{id}`, `GET /api/recipes/random`, `GET /api/recipes/cuisines`.

Note: `/random` and `/cuisines` route templates must be declared before `{id}` to avoid route conflicts.

---

## Phase 5 — Delay & Error Simulation (PRD §7) — ✅ DONE (via Phase 0 middleware)

**Already delivered in Phase 0.2** as middleware. This phase = verification pass across all resources + docs:
- Confirm `?delay=N` (cap 10s), `?error=CODE` for all 9 codes, combination `?delay=2&error=503`, and `X-Simulated: true` header.

---

## Phase 6 — Notifications & Transactions (PRD §8) — ✅ DONE

- **Notifications** `Models/Notification.cs` (id, userId, type, title, body, read, link, createdAt); `Data/NotificationStore.cs` (20 seed: mention/like/follow/system/order_update).
  - `GET /api/notifications` (`?userId=`, `?read=`), `GET /api/notifications/{id}`, `PUT /api/notifications/{id}/read`, `PUT /api/notifications/read-all`.
- **Transactions** `Models/Transaction.cs` (id, userId, type, amount, currency, description, merchant, category, date, balance); `Data/TransactionStore.cs` (40 seed).
  - `GET /api/transactions` (`?userId=`, `?type=`, `?category=`), `GET /api/transactions/{id}`, `GET /api/transactions/summary` (aggregate income/expenses/balance, `?userId=`).

---

## Cross-cutting standards (all phases)

- **Success:** GET single → object; GET list → array + pagination headers; POST → 201 `{ message, data }`; PUT → 200 `{ message, data }`; DELETE → 200 `{ message }`.
- **Errors:** `{ status, error, message }`; simulated errors add `simulated: true` + `X-Simulated: true`.
- **Headers on every response:** CORS set, `X-Powered-By: APIBee`, informational `X-RateLimit-*`, pagination `X-*` on list endpoints.

## Seed data summary

| Resource | Count | Phase | References |
|---|---|---|---|
| Products | 20 | 1 | — |
| Posts | 15 | 2 | Users |
| Comments | 50 | 2 | Posts, Users |
| Todos | 30 | 2 | Users |
| Carts | 8 | 3 | Users, Products |
| Orders | 12 | 3 | Users, Products |
| Quotes | 50 | 4 | — |
| Recipes | 15 | 4 | — |
| Notifications | 20 | 6 | Users |
| Transactions | 40 | 6 | Users |

**Total: ~260 new records across 10 new resources** (+ existing 5 Users).

## Suggested build order

Phase 0 → 1 (ship: covers e-commerce + all query features) → 2 (covers 80% of tutorial use cases) → then 3, 4, 6 in any order. Phase 5 is verification only since the middleware lands in Phase 0.

## Open questions / decisions

- **Thread-safety:** static `List<T>` mutated by writes isn't thread-safe. Acceptable for a dummy API per PRD; note if concurrency becomes a concern.
- **`createdAt`/timestamps:** seed with fixed ISO strings from the PRD examples (deterministic), not `DateTime.Now`.
- **Reflection in QueryHelper:** simple and generic; if perf ever matters (it won't at this scale) it can be specialized per type.
