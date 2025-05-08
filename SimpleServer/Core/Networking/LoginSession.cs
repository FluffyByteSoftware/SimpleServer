using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluffyByte.SimpleServer.Core.Networking;

namespace SimpleServer.Core.Networking
{
    public enum LoginState { AwaitingUserName, AwaitingPassword, Authorized, Rejected }

    internal class LoginSession
    {
        public LoginState State { get; private set; } = LoginState.AwaitingUserName;
        public int FailureCount { get; private set; } = 0;
        public string? Username { get; private set; } = "unknown";

        private const int MaxFailures = 5;

        public string GetPrompt()
        {
            return State switch
            {
                LoginState.AwaitingUserName => "Please enter your username",
                LoginState.AwaitingPassword => "Please enter your password",
                _ => ""
            };
        }

        public async Task<LoginProcessResult> ProcessInput(SocketClient sClient, string input)
        {
            switch (State)
            {
                case LoginState.AwaitingUserName:
                    if (input.Length > 3)
                    {
                        Username = input;
                        State = LoginState.AwaitingPassword;
                        return new(false, true); // advance and prompt next
                    }
                    else
                    {
                        await sClient.SendMessage("Invalid username.");
                        FailureCount++;

                        if (FailureCount >= MaxFailures)
                        {
                            await sClient.SendMessage("Too many invalid attempts... Disconnecting.");
                            await sClient.TryDisconnect();
                            State = LoginState.Rejected;
                            return new(false, false, true);
                        }

                        return new(false, true);
                    }

                case LoginState.AwaitingPassword:
                    if (input.Length > 3)
                    {
                        State = LoginState.Authorized;

                        if (string.IsNullOrEmpty(Username))
                            Username = "Unknown";

                        sClient.SetName(Username);
                        await sClient.ToggleAuthorized(Username);

                        return new(true, false);
                    }
                    else
                    {
                        await sClient.SendMessage("Invalid password.");
                        FailureCount++;

                        if (FailureCount >= MaxFailures)
                        {
                            await sClient.SendMessage("Too many invalid attempts... Disconnecting.");
                            await sClient.TryDisconnect();
                            State = LoginState.Rejected;
                            return new(false, false, true);
                        }

                        return new(false, true);
                    }
            }

            // fallback (should never hit this)
            return new(false, false);
        }

        public async Task<LoginProcessResult> ProcessInput(SimpleClient sClient, string input)
        {
            switch (State)
            {
                case LoginState.AwaitingUserName:
                    if (input.Length > 3)
                    {
                        Username = input;
                        State = LoginState.AwaitingPassword;
                        return new(false, true); // advance and prompt next
                    }
                    else
                    {
                        await sClient.SendMessage("Invalid username.");
                        FailureCount++;

                        if (FailureCount >= MaxFailures)
                        {
                            await sClient.SendMessage("Too many invalid attempts... Disconnecting.");
                            await sClient.TryDisconnect();
                            State = LoginState.Rejected;
                            return new(false, false, true);
                        }

                        return new(false, true);
                    }

                case LoginState.AwaitingPassword:
                    if (input.Length > 3)
                    {
                        State = LoginState.Authorized;

                        if (string.IsNullOrEmpty(Username))
                            Username = "Unknown";

                        sClient.SetName(Username);
                        await sClient.ToggleAuthorized(Username);

                        return new(true, false);
                    }
                    else
                    {
                        await sClient.SendMessage("Invalid password.");
                        FailureCount++;

                        if (FailureCount >= MaxFailures)
                        {
                            await sClient.SendMessage("Too many invalid attempts... Disconnecting.");
                            await sClient.TryDisconnect();
                            State = LoginState.Rejected;
                            return new(false, false, true);
                        }

                        return new(false, true);
                    }
            }

            // fallback (should never hit this)
            return new(false, false);
        }


        public bool IsCompleted => State == LoginState.Authorized || State == LoginState.Rejected;
    }
}
