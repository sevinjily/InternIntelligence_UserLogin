﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Utilities.Message.Abstract
{
   public interface IMessageService
    {
        Task SendMessage(string to, string subject, string message);

    }
}
