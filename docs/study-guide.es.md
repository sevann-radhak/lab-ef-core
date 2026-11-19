# Guía de estudio — EF Core

La versión en inglés (`study-guide.md`) es la canónica del repo. Esta copia está para estudiar en español.

La explicación larga está en [`senior-concepts.es.md`](./senior-concepts.es.md).

Objetivo: ver cómo una consulta LINQ se convierte en SQL, y cuándo un cambio en memoria llega a la base.

## Temas

1. N+1 y carga de relaciones
2. Proyecciones
3. `IQueryable`, `IEnumerable` y `List`
4. Change tracking y `AsNoTracking`

Para cada tema, prepará:

1. Una explicación clara (60–90 segundos)
2. Un recorrido de un endpoint de este repo
3. Una falla de producción y cómo la confirmarías

## Ejercicios

1. Llamá `GET /orders/n-plus-one` y `GET /orders/projected`. Compará `queryCount` y el SQL del log.
2. Llamá `GET /orders/include`. Con el log, decidí si el SQL es un join o una consulta partida.
3. Llamá `GET /orders/composed?customer=Ada%20Bakery` y `GET /orders/in-memory?customer=Ada%20Bakery`. Los dos devuelven dos pedidos y `queryCount` 1. Compará `loadedCount` y `execution`.
4. `POST /orders/1/note` con una nota nueva. `savedChanges` tiene que ser 1.
5. `POST /orders/1/note/detached` con otra nota. `savedChanges` tiene que ser 0 y `databaseNote` no debe tomar ese valor.
6. Agregá un pedido en `SalesSeed` y repetí el paso 1. El conteo charlatán sube. El de la proyección no.

## Para explicar

- `IQueryable` frente a `IEnumerable` y frente a `List`
- Include frente a Select
- Qué guarda el change tracker, y por qué un listado casi nunca lo necesita
- `SaveChanges` escribe solo lo que está trackeado y modificado
- Un 200 puede ser igual un N+1

## Límites

SQLite es el motor local. No muestra planes de SQL Server, locks, ni otra librería de acceso a datos.
