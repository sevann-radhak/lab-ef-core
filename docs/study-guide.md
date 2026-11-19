# Study guide — EF Core

Goal: see how a LINQ query becomes SQL, and when a change in memory becomes a database write.

## Topics

1. N+1 and loading
2. Projections
3. `IQueryable`, `IEnumerable`, and `List`
4. Change tracking and `AsNoTracking`

For each topic, prepare:

1. A clear explanation (60–90 seconds)
2. A walkthrough of one endpoint in this repository
3. A production failure and how you would confirm it

## Manual practice

1. Call `GET /orders/n-plus-one` and `GET /orders/projected`. Compare `queryCount` and the logged SQL.
2. Call `GET /orders/include`. Decide whether the SQL is one join or a split query, using the log.
3. Call `GET /orders/composed?customer=Ada%20Bakery` and `GET /orders/in-memory?customer=Ada%20Bakery`. Both return two orders and `queryCount` 1. Compare `loadedCount` and `execution`.
4. `POST /orders/1/note` with a new note. Confirm `savedChanges` is 1.
5. `POST /orders/1/note/detached` with a different note. Confirm `savedChanges` is 0 and `databaseNote` did not take that value.
6. Add one order in `SalesSeed` and repeat step 1. The chatty count should rise. The projection count should not.

## Talking points

- `IQueryable` versus `IEnumerable` versus `List`
- Include versus Select
- What the change tracker stores, and why a list endpoint usually does not need it
- `SaveChanges` writes only tracked modifications
- A 200 response can still be an N+1

## Limits

SQLite is the local engine. It does not demonstrate SQL Server plans, lock escalation, or a second data-access library.
