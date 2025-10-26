# Pruebas de Carga k6

## Objetivos Iniciales
1. Validar salud básica de la API (smoke).
2. Medir latencias base (baseline) de operaciones de datasets (listar y crear) con volumen moderado.
3. Generar volumen alto de datasets para evaluar comportamiento: p95, errores, degradación.
4. Identificar límites y cuellos de botella (stress). Más adelante.

## Estructura de Scripts
- `smoke.js`: Verifica login y un endpoint simple de listado de datasets (IM) con 1 VU en corto tiempo.
- `baseline-datasets.js`: Carga baja con ramping moderado midiendo listar y crear datasets IM.
- `seed-datasets.js`: Script utilitario para generar muchos datasets (solo crear). Controlado por variables de entorno.
- `dataset-volume.js`: Escenario lectura intensiva sobre grandes cantidades de datasets para evaluar impacto.

## Variables de Entorno Principales
- `BASE_URL` (ej: http://localhost:5000)
- `LOGIN_USER` / `LOGIN_PASS` (credenciales para /api/Auth/login)
- `TOKEN` (opcional si ya tienes un token válido y quieres saltarte login)
- `SEED_COUNT` (solo seed-datasets.js) cantidad aproximada de datasets a crear
- `DATASET_PREFIX` prefijo para nombres de dataset generados

## Endpoints Utilizados
Auth:
- POST `/api/Auth/login`

Datasets IM (`DatasetController`):
- POST `/api/Dataset` crear dataset IM
- GET `/api/Dataset/user?token=...&search=...` listar datasets IM del usuario
- GET `/api/Dataset/GetDataset?datasetId=ID&token=...` obtener uno
- DELETE `/api/Dataset/DeleteDataset?datasetId=ID&token=...` eliminar

Nota: El controlador usa `token` como query param para mapear a username vía `ISondaAuthService`.

## Métricas y Thresholds Iniciales
- `http_req_failed < 2%`
- `http_req_duration p(95) < 800ms` (baseline)
- Para volumen: `http_req_duration p(95) < 1200ms` (flexible mientras se optimiza)
- `checks rate > 97%`

## Ejecución Rápida
Smoke:
```powershell
k6 run -e BASE_URL=http://localhost:5000 -e LOGIN_USER=admin -e LOGIN_PASS=Secret123 load-tests/smoke.js
```
Baseline:
```powershell
k6 run -e BASE_URL=http://localhost:5000 -e LOGIN_USER=admin -e LOGIN_PASS=Secret123 load-tests/baseline-datasets.js
```
Seed (200 datasets):
```powershell
k6 run -e BASE_URL=http://localhost:5000 -e LOGIN_USER=admin -e LOGIN_PASS=Secret123 -e SEED_COUNT=200 -e DATASET_PREFIX=Perf load-tests/seed-datasets.js
```
Volume (lecturas intensivas):
```powershell
k6 run -e BASE_URL=http://localhost:5000 -e LOGIN_USER=admin -e LOGIN_PASS=Secret123 load-tests/dataset-volume.js
```

## Limpieza
Pendiente crear script `cleanup-datasets.js` para eliminar por prefijo. Próximo paso.

## Estrategia Incremental
1. Ejecutar seed para tener volumen (opcional usar datos reales).
2. Correr baseline y registrar métricas p50/p95 iniciales en README (tabla).
3. Ajustar thresholds si necesario.
4. Ejecutar volume y analizar degradación.
5. Planear stress (ramping > usuarios esperados).

## Próximos Pasos
- Script de limpieza.
- Escenarios k6 adicionales (stress, spike, soak).
- Exportar métricas a InfluxDB/Grafana.
- Integración CI (fallar build si thresholds no se cumplen).

## Notas
- Evitar crear datasets infinitos en seed; usar prefijos y cantidades controladas.
- Pensar en indexación BD si listar se degrada (revisar N+1, joins, filtros en memoria).
