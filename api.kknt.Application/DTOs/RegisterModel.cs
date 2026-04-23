using System.ComponentModel.DataAnnotations;

namespace api.kknt.Application.DTOs
{
    /// <summary>
    /// DTO cho luồng đăng ký tài khoản mới (thay cho RegisterViewModel cũ bên MVC).
    /// </summary>
    public class RegisterModel
    {
        public sealed class RegisterRequest
        {
            [Required, StringLength(20, MinimumLength = 10)]
            public string TaxCode { get; init; } = null!;

            [Required, StringLength(100)]
            public string CompanyName { get; init; } = null!;

            [Required, StringLength(100)]
            public string ContactName { get; init; } = null!;

            [Required, StringLength(500)]
            public string CmpnAddress { get; init; } = null!;

            [Required, StringLength(20)]
            public string CmpnPhone { get; init; } = null!;

            [Required, EmailAddress]
            public string Email { get; init; } = null!;

            [Required, MinLength(6)]
            public string Password { get; init; } = null!;

            /// <summary>optionFirstJob (tương đương model.chooseVal cũ).</summary>
            public string? ChooseVal { get; init; }
        }

        /// <summary>
        /// Kết quả đăng ký trả về cho FE (không chứa token – sau khi đăng ký khách
        /// phải login lại bằng MST + password).
        /// </summary>
        public record RegisterResponse(
            string TaxCode,
            string ServerHost,
            bool   IsNewServer
        );
    }
}
