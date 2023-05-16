using Entity.Entities;
using OpenAI.GPT3.ObjectModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class ChatRoleHelper
    {
        public static string GetRoleByInt(int role)
        {
            return role switch
            {
                0 => StaticValues.ChatMessageRoles.User,
                1 => StaticValues.ChatMessageRoles.Assistant,
                _ => throw new Exception($"There is no role - {role}")
            };
        }

        public static string GetRoleByEnum(ChatMessageRole role)
        {
            return role switch
            {
                ChatMessageRole.User => StaticValues.ChatMessageRoles.User,
                ChatMessageRole.Assistant => StaticValues.ChatMessageRoles.Assistant,
                _ => throw new Exception($"There is no role - {role}")
            };
        }


    }
}
