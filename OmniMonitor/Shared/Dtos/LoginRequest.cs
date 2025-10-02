using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    public class LoginRequest
    {
        //[Required(
        //    ErrorMessageResourceName = "RequiredUsername",
        //    ErrorMessageResourceType = typeof(SharedValidationMessages))]
        public string Username { get; set; } = string.Empty;

        //[Required(
        //    ErrorMessageResourceName = "RequiredPassword",
        //    ErrorMessageResourceType = typeof(SharedValidationMessages))]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
