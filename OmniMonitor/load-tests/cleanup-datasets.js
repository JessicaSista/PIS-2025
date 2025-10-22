import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

export const options = {
  vus: 2,
  duration: '2m',
  thresholds: {
    http_req_failed: ['rate<0.05'],
    checks: ['rate>0.90']
  },
  tags: { test_type: 'cleanup', domain: 'datasets' }
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const USER = __ENV.LOGIN_USER || 'admin';
const PASS = __ENV.LOGIN_PASS || 'Secret123';
const PREFIX = __ENV.DATASET_PREFIX || 'Perf';
const DRY_RUN = (__ENV.DRY_RUN || 'true').toLowerCase() === 'true';

const deleted = new Counter('datasets_deleted');
const candidates = new Counter('datasets_candidates');

function login() {
  if (__ENV.TOKEN) return __ENV.TOKEN;
  const res = http.post(`${BASE_URL}/api/Auth/login`, JSON.stringify({ username: USER, password: PASS }), {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'auth_login' }
  });
  check(res, { 'login 200': r => r.status === 200 });
  return res.json('token');
}

export function setup() {
  const token = login();
  return { token };
}

export default function (data) {
  const listRes = http.get(`${BASE_URL}/api/Dataset/user?token=${data.token}`, { tags: { endpoint: 'dataset_list' } });
  if (listRes.status !== 200) {
    sleep(1);
    return;
  }
  const arr = listRes.json();
  if (!Array.isArray(arr)) {
    sleep(1);
    return;
  }

  // Buscar candidatos por prefijo
  const toDelete = arr.filter(d => typeof d.name === 'string' && d.name.startsWith(PREFIX)).slice(0, 5); // máx 5 por iteración
  toDelete.forEach(d => {
    candidates.add(1);
    if (!DRY_RUN && d.id) {
      const delRes = http.del(`${BASE_URL}/api/Dataset/DeleteDataset?datasetId=${d.id}&token=${data.token}`, null, { tags: { endpoint: 'dataset_delete' } });
      const ok = check(delRes, { 'delete 204': r => r.status === 204 });
      if (ok) deleted.add(1);
    }
  });

  sleep(1);
}

export function teardown(data) {
  if (DRY_RUN) {
    console.log(`DRY_RUN activo. Para eliminar realmente usar -e DRY_RUN=false`);
  }
}
