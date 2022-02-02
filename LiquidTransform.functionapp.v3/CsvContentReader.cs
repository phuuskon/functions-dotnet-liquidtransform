using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DotLiquid;

namespace LiquidTransform.functionapp.v2
{
    public class CsvContentReader : IContentReader
    {
        public Hash ParseRequestAsync(string requestBody)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes( requestBody );
            var stream = new MemoryStream( byteArray );
            //var stream = await content.ReadAsStreamAsync();

            var transformInput = new Dictionary<string, object>();


            List<object[]> csv = new List<object[]>();

            StreamReader sr = new StreamReader(stream);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();

                csv.Add(line.Split(','));
            }

            transformInput.Add("content", csv.ToArray<object>());

            return Hash.FromDictionary(transformInput);
        }
    }
}
