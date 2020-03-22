using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Bookshelf.Core
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailsController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUserRepository _userRepository;
        private readonly IEmailHelper _emailHelper;

        public EmailsController(IConfiguration config, IUserRepository userRepository, IEmailHelper emailHelper)
        {
            _config = config;
            _userRepository = userRepository;
            _emailHelper = emailHelper;
        }

        [HttpPost]
        [Route("send-reset-token")]
        public ActionResult<PasswordResetDto> SendResetToken(PasswordResetDto model)
        {
            if(!_userRepository.UserPresent(model.Email))
            {
                return new PasswordResetDto
                {
                    Error = $"User with email {model.Email} does not exist."
                };
            }

            var user = _userRepository.GetUser(model.Email);
            var resetToken = Guid.NewGuid();
            var expiryDate = DateTime.Now.AddDays(1);
            _userRepository.SetPasswordResetFields(user.Id, resetToken, expiryDate);

            var url = $"{_config["SiteUrl"]}/{user.Id}/{resetToken}";
            _emailHelper.SendResetToken(model.Email, url);

            return model;
        }
    }
}