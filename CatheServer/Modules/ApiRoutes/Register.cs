using CatheServer.Modules.Database;
using Microsoft.AspNetCore.Builder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CatheServer.Modules.ApiRoutes
{
    public class Register
    {
        public static void RegisterRoutes(ref WebApplication app, DatabaseHandler database)
        {
            RegisterApiUser(ref app, database);
        }

        public static void RegisterApiUser(ref WebApplication app, DatabaseHandler database)
        {
            app.MapPost("/api/user/register", (context) => Utils.HttpResponseWrapper(context, async () =>
            {
                HttpResponseEntity? response = null;
                var content = await Utils.GetBodyContent(context);

                string username = (string)content["username"];
                string password = (string)content["password"];
                string email = (string)content["email"];

                byte[] passwordHash = SHA512.HashData(Encoding.UTF8.GetBytes(password));

                if (!await Utils.VerifyEmail(email))
                {
                    throw new ClientRequestInvalidException("Unable to find the MX record of the mail server corresponding to the email address. " +
                        $"Please ensure that \"{email.Split('@').Last()}\" is a valid domain, provides email services and has an MX record.");
                }

                UserEntity user = new UserEntity
                {
                    UserName = username,
                    Password = passwordHash,
                    Email = email
                };

                CheckIfDuplicated(database, username, email);

                database.AddEntity(user);

                int id = database.QueryByIndex<UserEntity>("username", username).FirstOrDefault()?.Id ?? -1;

                response = new HttpResponseEntity
                {
                    StatusCode = 200,
                    Data = new
                    {
                        id = id,
                        username = username,
                        password = Convert.ToBase64String(passwordHash),
                        email = email,
                        uuid = user.Uuid
                    }
                };

                return response;
            }));
            app.MapGet("/api/user/login", (context) => Utils.HttpResponseWrapper(context, async () =>
            {
                HttpResponseEntity? response = null;
                var content = await Utils.GetBodyContent(context);

                string password = (string)content["password"];
                string username = (string)content["username"];

                byte[] passwordHash = SHA512.HashData(Encoding.UTF8.GetBytes(password));

                UserEntity? user = database.QueryByIndex<UserEntity>("username", username).FirstOrDefault();

                if (user == null)
                {
                    throw new ClientRequestInvalidException($"User \"{username}\" not found.");
                }
                if (!Utils.EqualsAll(passwordHash, user.Password))
                {
                    throw new ClientRequestInvalidException($"Password incorrect.");
                }

                string jwtToken = user.GenerateJwtToken(CatheApiServer.rsa);

                response = new HttpResponseEntity
                {
                    StatusCode = 200,
                    Data = new
                    {
                        user = user,
                        token = jwtToken
                    }
                };

                return response;
            }));
            app.MapGet("/api/user/verify", (context) => Utils.HttpResponseWrapper(context, async () =>
            {
                HttpResponseEntity? response = null;
                var content = await Utils.GetBodyContent(context);

                string token = (string)content["token"];
                string username = (string)content["username"];

                bool isValid = UserEntity.VerifyJwtToken(CatheApiServer.rsa, token, username, out Exception? ex);

                response = new HttpResponseEntity
                {
                    StatusCode = isValid ? 200 : 401,
                    Message = isValid ? "success" : "failed",
                    Error = new Error
                    {
                        Message = ex?.Message,
                        Type = ex?.GetType().FullName
                    }
                };

                return response;
            }));
        }

        private static void CheckIfDuplicated(DatabaseHandler database, string username, string email)
        {
            if (database.QueryByIndex<UserEntity>("email", email).Count != 0 ||
                database.QueryByIndex<UserEntity>("username", username).Count != 0)
            {
                throw new ClientRequestInvalidException("Duplicated user name or email.");
            }
        }
    }
}
