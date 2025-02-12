﻿using System.Globalization;
using System.Security.Cryptography;
using CleanHr.Application.Infrastructures;
using CleanHr.Application.Services;
using CleanHr.Domain.Aggregates.IdentityAggregate;
using MediatR;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.Application.Commands.IdentityCommands.UserCommands;

public sealed class SendEmailVerificationCodeCommand : IRequest
{
    public SendEmailVerificationCodeCommand(string email)
    {
        Email = email.ThrowIfNotValidEmail(nameof(email));
    }

    public string Email { get; }

    private class SendEmailVerificationCodeCommandHanlder : IRequestHandler<SendEmailVerificationCodeCommand>
    {
        private readonly IRepository _repository;
        private readonly ViewRenderService _viewRenderService;
        private readonly IEmailSender _emailSender;

        public SendEmailVerificationCodeCommandHanlder(
            IRepository repository,
            ViewRenderService viewRenderService,
            IEmailSender emailSender)
        {
            _repository = repository;
            _viewRenderService = viewRenderService;
            _emailSender = emailSender;
        }

        public async Task<Unit> Handle(SendEmailVerificationCodeCommand request, CancellationToken cancellationToken)
        {
            request.ThrowIfNull(nameof(request));

            int randomNumber = RandomNumberGenerator.GetInt32(0, 1000000);
            string verificationCode = randomNumber.ToString("D6", CultureInfo.InvariantCulture);

            EmailVerificationCode emailVerificationCode = new EmailVerificationCode()
            {
                Code = verificationCode,
                Email = request.Email,
                SentAtUtc = DateTime.UtcNow
            };

            await _repository.AddAsync(emailVerificationCode, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            (string Email, string VerificationCode) model = (request.Email, verificationCode);
            string emailBody = await _viewRenderService.RenderViewToStringAsync("EmailTemplates/ConfirmRegistrationCodeTemplate", model);

            string senderEmail = "noreply@yourapp.com";
            string subject = "User Registration";

            EmailMessage emailObject = new EmailMessage(request.Email, request.Email, senderEmail, senderEmail, subject, emailBody);

            await _emailSender.SendAsync(emailObject);

            return Unit.Value;
        }
    }
}
