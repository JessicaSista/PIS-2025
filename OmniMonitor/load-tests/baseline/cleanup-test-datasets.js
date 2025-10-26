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
  // Obtener todos los datasets
  const res = http.get(`${BASE_URL}/api/Dataset/user?token=${data.token}`);

  check(res, {
    "status is 200": (r) => r.status === 200,
  });

  const datasets = res.json();

  // Filtrar datasets creados por la prueba de carga
  const testDatasets = datasets.filter((d) => d.name.startsWith("Perf_"));

  console.log(
    `\nEncontramos ${testDatasets.length} datasets de prueba para borrar.`
  );

  // Borrar cada dataset de prueba
  let deletedCount = 0;
  for (const dataset of testDatasets) {
    const deleteRes = http.del(
      `${BASE_URL}/api/Dataset/${dataset.id}?token=${data.token}`,
      null,
      {
        headers: { "Content-Type": "application/json" },
      }
    );

    if (deleteRes.status === 200 || deleteRes.status === 204) {
      deletedCount++;
      console.log(`✓ Borrado dataset: ${dataset.name} (ID: ${dataset.id})`);
    } else {
      console.log(
        `✗ Error al borrar dataset: ${dataset.name} (ID: ${dataset.id})`
      );
    }
  }

  console.log(`\nResumen:`);
  console.log(`- Total datasets de prueba encontrados: ${testDatasets.length}`);
  console.log(`- Datasets borrados exitosamente: ${deletedCount}`);
  console.log(
    `- Datasets con error al borrar: ${testDatasets.length - deletedCount}`
  );
}
