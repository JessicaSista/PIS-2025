namespace OmniMonitor.Server.Resources
{
    public static class Language
    {
        public const string InvalidData = "Datos de entrada inválidos";
        public const string InternalServerError = "Error interno del servidor";
        public const string UserNotFound = "Usuario no encontrado";
        public const string InvalidPassword = "Contraseña incorrecta";
        public const string LoginSuccess = "Login exitoso";
        public const string InternalErrorPrefix = "Error interno: ";

        // KPI Messages
        public const string KpiObjectNull = "El objeto KPI es nulo.";
        public const string InvalidToken = "Token inválido.";
        public const string DbError = "DB Error: ";
        public const string KpiNotFoundUser = "No se encontró el KPI con ID {0} para el usuario {1}.";
        public const string KpiDataRequired = "Los datos del KPI son requeridos.";
        public const string KpiNotFound = "No se encontró el KPI con ID {0}.";
        public const string KpiDeleted = "KPI con ID {0} eliminado correctamente.";
        public const string ModuleRequired = "Debe especificarse el módulo.";
        public const string MetricsNotFound = "No se encontraron métricas para el módulo {0}.";
        public const string MetricsError = "Error interno al obtener métricas: ";
        public const string NoDataForDateRange = "No se encontraron datos para el rango de fechas.";
        public const string ModuleNotSupported = "Tipo de módulo no soportado";
        public const string PageAndSizeMustBeGreaterThanZero = "La página y el tamaño deben ser mayores a 0.";
        public const string DatasetIdInvalid = "Debe especificar un ID de dataset válido.";
        public const string FieldRequired = "Debe especificar el campo.";
        public const string KpiNameExists = "Ya existe un KPI con el nombre '{0}'.";
        public const string ColorRangesInvalid = "ColorRanges inválido o vacío.";
        public const string MinMaxError = "El mínimo debe ser menor o igual al máximo en cada rango de color.";
        public const string ColorRangesJsonError = "ColorRanges no es JSON válido.";
        public const string KpiNameRequired = "KPI name is required.";
        public const string SourceModuleRequired = "SourceModule is required.";
        public const string DatasetIdRequired = "DatasetId is required.";
        public const string ExtraInfoRequiredIM = "ExtraInfo is required for IM KPIs.";
        public const string ExtraInfoParseError = "ExtraInfo could not be parsed.";
        public const string ExtraInfoDatesRequired = "ExtraInfo must contain dateFrom/dateTo or startDate/endDate.";
        public const string InvalidDateRange = "Invalid date range: 'dateFrom' must be earlier than or equal to 'dateTo'.";
        public const string DatasetNotFound = "Dataset with ID {0} not found.";
        public const string SourceNotFound = "Source with ID {0} not found.";
        public const string NoDevicesFound = "No devices found for source {0}.";
        public const string SensorNotFound = "Sensor '{0}' not found in source {1}.";
        public const string UnsupportedMetricIM = "Unsupported metric '{0}' for IM KPIs.";
        public const string UnauthorizedDeleteKpi = "No tiene permisos para eliminar este KPI.";
        public const string UnauthorizedEditKpi = "No tiene permisos para editar este KPI.";
        public const string NameEmpty = "Name provisto pero vacío.";
        public const string SourceModuleEmpty = "SourceModule provisto pero vacío.";
        public const string DatasetIdPositive = "DatasetId debe ser mayor que 0.";
        public const string UnitEmpty = "Unit provisto pero vacío.";
        public const string MetricEmpty = "Metric provisto pero vacío.";
        public const string MultiplierPositive = "Multiplier debe ser mayor que 0.";
        public const string InvalidHexColor = "DefaultColor no es un color hex válido (ej. #RRGGBB).";
        public const string ExtraInfoRequiredMetric = "ExtraInfo requerida para la métrica '{0}'.";
        public const string ExtraInfoDatesIso = "ExtraInfo debe contener dateFrom y dateTo en formato ISO.";
        public const string ExtraInfoJsonError = "ExtraInfo no es JSON válido.";
        public const string ExtraInfoDatesFormat = "Las fechas en ExtraInfo no tienen un formato válido (ISO).";
        public const string SourceModuleNotSupported = "SourceModule no soportado: {0}";
        public const string KpiCalculationError = "No se pudo calcular el KPI con ID {0}";
        public const string KpiCalculationErrorData = "No se pudo calcular el KPI con los datos proporcionados";
        public const string DatasetNotFoundKpi = "No se encontró el dataset con ID {0} para el KPI {1}";
        public const string MetricNotSupportedAM = "Métrica no soportada para AM: {0}";
        public const string MetricNotSupportedIM = "Métrica no soportada para IM: {0}";
        public const string DatasetNotFoundSimple = "Dataset no encontrado";
        public const string AttributeNotSupportedStock = "Atributo no soportado para Stock: {0}";

        // Dashboard Messages
        public const string DashboardCreateError = "Error interno al crear el dashboard: {0}";
        public const string DashboardNotFoundUser = "No se encontró el dashboard con ID {0} para el usuario {1}.";
        public const string DashboardGetError = "Error interno al obtener el dashboard: {0}";
        public const string DashboardNotFound = "No se encontró el dashboard con ID {0}";
        public const string DashboardsGetError = "Error interno al obtener los dashboards: {0}";
        public const string PageSizeInvalid = "La página y el tamaño deben ser mayores a 0.";
        public const string CardIdsEmpty = "La lista de IdVisualizacion no puede estar vacía";
        public const string CardIdsValid = "Todos los IdVisualizacion son válidos";
        public const string CardIdsInvalid = "Algunos IdVisualizacion no existen en el sistema";
        public const string CardIdsValidationError = "Error interno al validar los IdVisualizacion: {0}";
        public const string DashboardDeleted = "Dashboard con id {0} eliminado correctamente para el usuario '{1}'";
        public const string DashboardConfigUpdated = "Configuración actualizada correctamente para el dashboard {0}";
        public const string DashboardCardAdded = "Tarjeta agregada correctamente al dashboard {0}";
        public const string DashboardCardAddError = "Error interno al agregar la tarjeta: {0}";
        public const string DashboardCardsReordered = "Orden de tarjetas actualizado correctamente para el dashboard {0}";
        public const string DashboardCardNotFound = "No se encontró la tarjeta con idCard {0} y tipoCard {1} en el dashboard {2} para el usuario '{3}'";
        public const string DashboardCardDeleted = "Tarjeta eliminada correctamente del dashboard {0}";
        public const string DashboardInfoUpdateError = "Error interno al actualizar la información del dashboard: {0}";
        public const string DashboardCardEditInvalid = "Datos inválidos para la edición de la tarjeta.";
        public const string DashboardCardEditNotFound = "No se encontró la tarjeta o la visualización asociada para editar.";
        public const string DashboardCardEditSuccess = "Tarjeta y visualización actualizadas correctamente.";
        public const string TokenInvalid = "Token inválido.";
        public const string ShareLinkCreateError = "Ocurrió un error interno al crear el enlace.";
        public const string InternalError = "Ocurrió un error interno.";
        public const string ShareLinkNotFound = "Enlace no encontrado, inválido o expirado.";
        public const string InternalErrorDetails = "Error interno.";
        public const string ShareLinkNotFoundOrUnauthorized = "Enlace no encontrado o no autorizado para este usuario.";
        public const string DashboardNameExists = "Ya existe un dashboard con el nombre '{0}' para el usuario '{1}'.";
        public const string VisualIdNotFound = "Uno o más IdVisualizacion no existen en el sistema.";
        public const string KpiIdNotFound = "Uno o más KPI no existen en el sistema.";
        public const string DashboardCreateRetrieveError = "Error al recuperar el dashboard creado.";
        public const string VisualIdNotFoundSingle = "No existe una visualización con Id {0}";
        public const string KpiIdNotFoundSingle = "No existe un KPI con Id {0}";
        public const string CardDuplicate = "Tarjeta duplicada: ya existe una tarjeta con ese IdVisualizacion y TipoCard en el dashboard.";
        public const string JsonConfigEmpty = "El JSON de configuración no puede estar vacío.";
        public const string DashboardNotFoundOrUnauthorized = "Dashboard no encontrado o no pertenece al usuario.";

        // Dataset Messages
        public const string DatasetCreateError = "Error interno al crear el dataset: {0}";
        public const string DatasetCreateFilteredError = "Error interno al crear el dataset filtrado: {0}";
        public const string DatasetGetError = "Error interno al obtener los datasets: {0}";
        public const string DatasetGetByIdError = "Error interno al obtener el dataset: {0}";
        public const string DatasetUpdateError = "Error interno al actualizar el dataset: {0}";
        public const string DatasetDeleteError = "Error interno al eliminar el dataset: {0}";
        public const string DatasetModuleError = "Error interno al identificar el módulo: {0}";
        public const string DatasetSensorTypeError = "Error interno al obtener el tipo del sensor: {0}";
        public const string DatasetNameRequired = "El nombre de usuario y el nombre del dataset son obligatorios.";
        public const string DatasetNameExists = "Ya existe un dataset con el nombre '{0}' para el usuario '{1}'.";
        public const string DatasetFilteredOnly = "Este endpoint es solo para datasets no formales (IsDataset = 'N'). Use el endpoint regular para datasets formales.";
        public const string DatasetFormalOnly = "Este método es solo para datasets no formales (IsDataset = 'N').";
        public const string ContentTypeInvalid = "ContentType inválido o no soportado";
        public const string FilterNoResults = "El filtro no encontró ningún {0}. El dataset no puede crearse sin resultados.";
        public const string FilterNoResultsUpdate = "El filtro no encontró ningún {0}. El dataset no puede actualizarse sin resultados.";
        public const string DatasetInsufficientInfo = "El dataset no contiene información suficiente (Source o SensorName).";
        public const string SensorNotFoundInDevice = "No se encontró el sensor '{0}' en ningún device del Source.";
        public const string DatasetCannotBeNull = "El dataset no puede ser nulo.";
        public const string DatasetSaveError = "Error al guardar el dataset en la base de datos: {0}. Inner Exception: {1}";
        public const string ContentTypeInvalidFiltered = "ContentType no válido para datasets filtrados.";
        public const string ModuleNotDefined = "Módulo no definido";
        public const string EntityNotDefined = "Entidad no definida para el módulo seleccionado";

        // Sonda API Error Messages
        public const string ApiUnauthorized = "No tienes permisos: token inválido o expirado (401 Unauthorized).";
        public const string ApiForbidden = "No tienes permisos para acceder a este recurso (403 Forbidden).";
        public const string ApiResponseEmpty = "La respuesta de la API está vacía.";
        public const string ApiResponseInvalidJson = "La respuesta de la API no es JSON válido. Respuesta: {0}";
        public const string ApiNotFound = "No se encontraron {0} (404 NotFound).";
        public const string ParameterMustBePositive = "El parámetro '{0}' debe ser mayor que cero.";
        public const string ParameterRequired = "El parámetro '{0}' es requerido.";
        public const string AssetNotFound = "AssetNotFound";
        public const string DeserializationError = "Error al deserializar la respuesta de la API: JSON inválido.";
    }
}
