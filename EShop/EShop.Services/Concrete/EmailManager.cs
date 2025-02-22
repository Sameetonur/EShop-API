using System;
using System.Net;
using System.Net.Mail;
using Azure;
using EShop.Services.Abstract;
using EShop.Shared.Configurations.Email;
using EShop.Shared.Dtos.ResponseDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Microsoft.IdentityModel.Tokens;

namespace EShop.Services.Concrete;

public class EmailManager : IEmailService
{
    private readonly EmailConfig _emailconfig;

    public EmailManager(IOptions<EmailConfig> emailconfig)
    {
        _emailconfig = emailconfig.Value;
    }

    public async Task<ResponseDto<NoContent>> SendEmailAsync(string emailTo, string subject, string htmlBody)
    {
        try
        {
            if (string.IsNullOrEmpty(_emailconfig.SmtpServer))
            {
                return ResponseDto<NoContent>.Fail("Smtp Sunucu adresi yapılandırılmamış!",StatusCodes.Status500InternalServerError);
                
            }
            if (string.IsNullOrEmpty(_emailconfig.SmtpUser))
            {
                return ResponseDto<NoContent>.Fail("Smtp Kullanıcı adı bilgisi tanımlanmamış", StatusCodes.Status500InternalServerError);

            }
            if (string.IsNullOrEmpty(_emailconfig.SmtpPasword))
            {
                return ResponseDto<NoContent>.Fail("Smtp Şifresi yapılandırılmamış!", StatusCodes.Status500InternalServerError);

            }
            if (string.IsNullOrEmpty(emailTo))
            {
                return ResponseDto<NoContent>.Fail("Alıcı email adresi boş olamaz!", StatusCodes.Status400BadRequest);

            }
            if (!IsValidEmail(emailTo))
            {
                return ResponseDto<NoContent>.Fail(" Gersiz email formatı!", StatusCodes.Status400BadRequest);

            }

            using var SmtpClient = new SmtpClient(_emailconfig.SmtpServer, _emailconfig.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailconfig.SmtpUser, _emailconfig.SmtpPasword),
                EnableSsl = false,
                Timeout = 20000 //20 saniye
            };

            var mailMessage = new MailMessage{
                From = new MailAddress(_emailconfig.SmtpUser),
                Subject = subject,
                Body=htmlBody,
                IsBodyHtml=true,
                To = {new MailAddress(emailTo)}

            };
             await SmtpClient.SendMailAsync(mailMessage);

              return ResponseDto<NoContent>.Success(StatusCodes.Status200OK);


        }
        catch (SmtpException smptex)
        {
            
            return ResponseDto<NoContent>.Fail(smptex.Message,StatusCodes.Status502BadGateway);//yanlış yol hatası
        }
        catch(Exception ex)
        {
            return ResponseDto<NoContent>.Fail(ex.Message,StatusCodes.Status500InternalServerError);
        }
    }

        private bool IsValidEmail(string emailAddress)
        {
            try
            {
                var addr = new MailAddress(emailAddress);
                return addr.Address==emailAddress;
            }
            catch (System.Exception)
            {
                return false;
                
            }
        }
}
