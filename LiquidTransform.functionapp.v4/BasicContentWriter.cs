﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LiquidTransform.functionapp.v2
{
    public class BasicContentWriter : IContentWriter
    {
        string _contentType;

        public BasicContentWriter(string contentType)
        {
            _contentType = contentType;
        }

        public object CreateResponse(string output)
        {
            return output;
        }
    }
}
