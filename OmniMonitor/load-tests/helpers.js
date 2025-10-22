// Helpers comunes para scripts k6 futuros
export function randomString(len = 8) {
  return Math.random().toString(36).substring(2, 2 + len);
}

export function buildDatasetPayload(name, username) {
  return {
    name,
    username,
    description: 'Generado por pruebas de carga',
    isDataset: 'S',
    contentType: '0',
    sourceId: null,
    groupId: null,
    sensorName: null,
    deviceIds: []
  };
}
