using System.Net;
using System.Net.Mail;
using System.Text;
using api.kknt.Application.DTOs;
using api.kknt.Application.InterfaceServices;
using api.kknt.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api.kknt.Application.ImplementService
{
    public class RegistrationEmailService : IRegistrationEmailService
    {
        private readonly RegistrationNotificationOptions _opt;
        private readonly ILogger<RegistrationEmailService> _logger;

        public RegistrationEmailService(
            IOptions<RegistrationNotificationOptions> opt,
            ILogger<RegistrationEmailService> logger)
        {
            _opt = opt.Value;
            _logger = logger;
        }

        public Task<bool> SendCustomerAsync(RegistrationEmailContext ctx, CancellationToken ct = default)
            => SendAsync(
                templateFile: _opt.CustomerTemplate,
                to: ctx.CustomerEmail,
                subject: "[KKNT] Xác nhận đăng ký tài khoản thành công",
                ctx: ctx,
                ct: ct);

        public Task<bool> SendSaleAsync(RegistrationEmailContext ctx, CancellationToken ct = default)
            => SendAsync(
                templateFile: _opt.SaleTemplate,
                to: _opt.SaleTo,
                subject: $"[KKNT][KD] Khách mới đăng ký — {ctx.CompanyName} ({ctx.TaxCode})",
                ctx: ctx,
                ct: ct);

        public Task<bool> SendSupportAsync(RegistrationEmailContext ctx, CancellationToken ct = default)
            => SendAsync(
                templateFile: _opt.SupportTemplate,
                to: _opt.SupportTo,
                subject: $"[KKNT][HT] Cần setup — {ctx.CompanyName} ({ctx.TaxCode})",
                ctx: ctx,
                ct: ct);

        public async Task SendAllSafeAsync(RegistrationEmailContext ctx, CancellationToken ct = default)
        {
            if (!_opt.Enabled)
            {
                _logger.LogInformation("[Email] Đã tắt (Enabled=false). Bỏ qua MST={Tax}", ctx.TaxCode);
                return;
            }
            // Chạy song song để giảm latency; không throw (đã nuốt lỗi trong SendAsync)
            var tasks = new[]
            {
                SendCustomerAsync(ctx, ct),
                SendSaleAsync(ctx, ct),
                SendSupportAsync(ctx, ct)
            };

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "[Email] MST={Tax} KH={Kh} KD={Kd} HT={Ht}",
                ctx.TaxCode,
                tasks[0].Result ? "OK" : "FAIL",
                tasks[1].Result ? "OK" : "FAIL",
                tasks[2].Result ? "OK" : "FAIL");
        }

        #region Helpers Private
        
        private async Task<bool> SendAsync(
            string templateFile,
            string? to,
            string subject,
            RegistrationEmailContext ctx,
            CancellationToken ct)
        {
            if (!_opt.Enabled) return false;

            try
            {
                if (string.IsNullOrWhiteSpace(to))
                {
                    _logger.LogWarning("[Email] Không có địa chỉ nhận cho template {Tpl}. MST={Tax}",
                        templateFile, ctx.TaxCode);
                    return false;
                }

                var body = await LoadTemplateAsync(templateFile);
                if (body is null) return false;

                body = FillPlaceholders(body, ctx);

                using var smtp = BuildSmtp();
                using var msg = BuildMessage(to!, subject, body);

                await smtp.SendMailAsync(msg, ct);

                _logger.LogInformation(
                    "[Email] Sent {Tpl} to {To} — MST={Tax}",
                    templateFile, to, ctx.TaxCode);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Email] FAIL {Tpl} to {To} — MST={Tax}", templateFile, to, ctx.TaxCode);
                return false;
            }
        }

        private async Task<string?> LoadTemplateAsync(string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, _opt.TemplateDir, fileName);
            if (!File.Exists(path))
            {
                _logger.LogWarning("[Email] Template không tồn tại: {Path}", path);
                return null;
            }
            return await File.ReadAllTextAsync(path);
        }

        private string FillPlaceholders(string body, RegistrationEmailContext c)
        {
            var expiry = c.RegisterAt.AddDays(_opt.TrialDays).ToString("dd/MM/yyyy");
            var serverStatus = c.IsNewServer
                ? "MST mới — server mặc định"
                : "MST đã tồn tại — dùng server cũ";

            return body
                .Replace("##cusName##", c.ContactName)
                .Replace("##cusEmail##", c.CustomerEmail)
                .Replace("##cusPhone##", c.CustomerPhone)
                .Replace("##cusAddress##", c.CustomerAddress)
                .Replace("##cmpnName##", c.CompanyName)
                .Replace("##taxCode##", c.TaxCode)
                .Replace("##registerDate##", c.RegisterAt.ToString("dd/MM/yyyy HH:mm"))
                .Replace("##serverHost##", c.ServerHost)
                .Replace("##serverStatus##", serverStatus)
                .Replace("##trialDays##", _opt.TrialDays.ToString())
                .Replace("##expiryDate##", expiry)
                .Replace("##loginUrl##", _opt.LoginUrl ?? "")
                .Replace("##supportPhone##", _opt.SupportPhone ?? "")
                .Replace("##supportEmail##", _opt.SupportEmail ?? "")
                .Replace("##tendonvi##", _opt.TenDonVi ?? "")
                .Replace("##thisYear##", c.RegisterAt.Year.ToString())
                .Replace("##saleName##", c.SaleName ?? "(đang phân công)")
                .Replace("##saleEmail##", c.SaleEmail ?? "")
                .Replace("##salePhone##", c.SalePhone ?? "");
        }

        private SmtpClient BuildSmtp() => new(_opt.SmtpHost, _opt.SmtpPort)
        {
            EnableSsl = _opt.EnableSsl,
            Credentials = new NetworkCredential(_opt.Username, _opt.Password)
        };

        private MailMessage BuildMessage(string to, string subject, string body)
        {
            var msg = new MailMessage
            {
                From = new MailAddress(_opt.From),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            foreach (var addr in to.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                msg.To.Add(addr.Trim());

            return msg;
        }
    }
    #endregion
}