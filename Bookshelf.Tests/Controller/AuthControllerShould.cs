using System;
using System.Net;
using Bookshelf.Core;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Bookshelf.Tests
{
    [TestFixture]
    public class AuthControllerShould
    {
        LoginDtoValidator _loginDtoValidator => new LoginDtoValidator();

        [Test]
        public void ReturnToken_WhenAuthorizedUserCallsLogin()
        {
            var login = new LoginDto
            {
                Email = "test@gmail.com",
                Password = "test"
            };

            var result = new TokenDto
            {
                Token = "test"
            };

            var userRepository = A.Fake<IUserRepository>();
            A.CallTo(() => userRepository.UserPresent(login.Email)).Returns(true);

            var userHelper = A.Fake<IUserHelper>();
            A.CallTo(() => userHelper.PasswordsMatch(login.Password, A<string>.Ignored, null)).Returns(true);
            A.CallTo(() => userHelper.BuildToken(A<UserDto>.Ignored)).Returns("test");

            var usersController = new AuthController(userRepository, userHelper, _loginDtoValidator);

            var responseOne = usersController.Login(login);
            var responseTwo = usersController.Login(new LoginDto());

            Assert.AreEqual(result.Token, responseOne.Value.Token);
            Assert.AreEqual((int)HttpStatusCode.BadRequest, ((BadRequestObjectResult)responseTwo.Result).StatusCode);
        }

        [Test]
        public void ReturnError_WhenUnauthorizedEmailCallsLogin()
        {
            var login = new LoginDto
            {
                Email = "test@gmail.com",
                Password = "test"
            };

            var userRepository = A.Fake<IUserRepository>();
            A.CallTo(() => userRepository.UserPresent(login.Email)).Returns(false);
            var usersController = new AuthController(userRepository, null, _loginDtoValidator);

            var response = usersController.Login(login);

            Assert.Null(response.Value.Token);
            Assert.AreEqual("Incorrect email address. Please try again.", response.Value.Error);
        }

        [Test]
        public void ReturnToken_WhenRegisterModelCorrect()
        {
            var register = new LoginDto
            {
                Email = "test@gmail.com",
                Password = "test"
            };

            var result = new TokenDto
            {
                Token = "test"
            };

            var userRepository = A.Fake<IUserRepository>();
            A.CallTo(() => userRepository.UserPresent(A<string>.Ignored)).Returns(false);
            
            var userHelper = A.Fake<IUserHelper>();
            A.CallTo(() => userHelper.BuildToken(A<UserDto>.Ignored)).Returns("test");

            var usersController = new AuthController(userRepository, userHelper, _loginDtoValidator);

            var responseOne = usersController.Register(register);
            var responseTwo = usersController.Register(new LoginDto());

            Assert.AreEqual(result.Token, responseOne.Value.Token);
            Assert.AreEqual((int)HttpStatusCode.BadRequest, ((BadRequestObjectResult)responseTwo.Result).StatusCode);
        }

        [Test]
        public void ReturnError_WhenRegisteringUsernameAlreadyExists()
        {
            var register = new LoginDto
            {
                Email = "test@gmail.com",
                Password = "test"
            };

            var userRepository = A.Fake<IUserRepository>();
            A.CallTo(() => userRepository.UserPresent(A<string>.Ignored)).Returns(true);

            var usersController = new AuthController(userRepository, null, _loginDtoValidator);

            var response = usersController.Register(register);

            Assert.Null(response.Value.Token);
            Assert.AreEqual("Email already in use. Please try another.", response.Value.Error);
        }

        [Test]
        public void ReturnUserDto_WhenResetTokenValid_InCallToUpdatePasswordUsingToken()
        {
            var model = new ResetTokenUpdateDto 
            {
                UserId = 1,
                Token = Guid.NewGuid(),
                Password = "test"
            };
            
            var user = new UserDto
            {
                Id = 1
            };

            var userRepository = A.Fake<IUserRepository>();
            A.CallTo(() => userRepository.UserPresent(model.UserId)).Returns(true);
            A.CallTo(() => userRepository.GetUser(model.UserId)).Returns(user);

            var userHelper = A.Fake<IUserHelper>();
            A.CallTo(() => userHelper.ValidResetToken(user, model.Token)).Returns(true);

            var controller = new AuthController(userRepository, userHelper, null);

            var response = controller.UpdatePasswordUsingToken(model);

            A.CallTo(() => userRepository.UpdatePasswordHash(model.UserId, A<string>.Ignored)).MustHaveHappened();
            A.CallTo(() => userRepository.SetPasswordResetFields(model.UserId, null, null)).MustHaveHappened();
            Assert.AreEqual(model.UserId, response.Value.Id);
        }

        [Test]
        public void ReturnBadRequest_WhenUserNotPresent_InCallToUpdatePasswordUsingToken()
        {
            var model = new ResetTokenUpdateDto 
            {
                UserId = 1,
                Token = Guid.NewGuid(),
                Password = "test"
            };

            var userRepository = A.Fake<IUserRepository>();
            A.CallTo(() => userRepository.UserPresent(model.UserId)).Returns(false);

            var controller = new AuthController(userRepository, null, null);

            var response = controller.UpdatePasswordUsingToken(model);

            Assert.AreEqual((int)HttpStatusCode.BadRequest, ((BadRequestObjectResult)response.Result).StatusCode);
            Assert.AreEqual($"User with Id {model.UserId} does not exist.", ((BadRequestObjectResult)response.Result).Value);
        }

        [Test]
        public void ReturnBadRequest_WhenTokenInvalid_InCallToUpdatePasswordUsingToken()
        {
            var model = new ResetTokenUpdateDto 
            {
                UserId = 1,
                Token = Guid.NewGuid(),
                Password = "test"
            };

            var user = new UserDto
            {
                Id = 1
            };

            var userRepository = A.Fake<IUserRepository>();
            A.CallTo(() => userRepository.UserPresent(model.UserId)).Returns(true);
            A.CallTo(() => userRepository.GetUser(model.UserId)).Returns(user);

            var userHelper = A.Fake<IUserHelper>();
            A.CallTo(() => userHelper.ValidResetToken(user, model.Token)).Returns(false);

            var controller = new AuthController(userRepository, userHelper, null);

            var response = controller.UpdatePasswordUsingToken(model);

            Assert.AreEqual((int)HttpStatusCode.BadRequest, ((BadRequestObjectResult)response.Result).StatusCode);
            Assert.AreEqual("Password reset token is not valid.", ((BadRequestObjectResult)response.Result).Value);
        }
    }
}