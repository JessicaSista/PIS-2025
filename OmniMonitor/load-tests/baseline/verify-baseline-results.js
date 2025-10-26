import http from "k6/http";
import { check } from "k6";

const BASE_URL =
  "https://web-smartplatform-dev-c0cdendhffa2ghf2.mexicocentral-01.azurewebsites.net/";
const USER = "admin";
const PASS = "admin";

export function setup() {
  const res = http.post(
    `${BASE_URL}/api/Auth/login`,
    JSON.stringify({ username: USER, password: PASS }),
    {
      headers: { "Content-Type": "application/json" },
    }
  );
  return { token: res.json("token") };
}

export default function (data) {
  const res = http.get(`${BASE_URL}/api/Dataset/user?token=${data.token}`);

  // Verificar status code
  check(res, {
    "status is 200": (r) => r.status === 200,
  });

  const datasets = res.json();

  // Verificar que es un array
  check(datasets, {
    "response is array": (r) => Array.isArray(r),
  });

  // Filtrar datasets creados por la prueba de carga (prefijo 'Perf_')
  const testDatasets = datasets.filter((d) => d.name.startsWith("Perf_"));

  // Verificaciones sobre los datasets de prueba
  check(testDatasets, {
    "hay datasets de prueba": (arr) => arr.length > 0,
    "todos los datasets tienen prefijo correcto": (arr) =>
      arr.every((d) => d.name.startsWith("Perf_")),
  });

  console.log(`Total de datasets en el sistema: ${datasets.length}`);
  console.log(
    `Datasets creados por la prueba de carga: ${testDatasets.length}`
  );
  console.log("\nÚltimos 5 datasets de prueba creados:");
  testDatasets
    .slice(-5)
    .forEach((d) => console.log(`- ${d.name} (ID: ${d.id})`));
}
