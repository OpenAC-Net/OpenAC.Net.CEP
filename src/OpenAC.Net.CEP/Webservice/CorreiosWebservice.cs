// ***********************************************************************
// Assembly         : OpenAC.Net.CEP
// Author           : RFTD
// Created          : 02-26-2017
//
// Last Modified By : RFTD
// Last Modified On : 02-26-2017
// ***********************************************************************
// <copyright file="CorreiosWebservice.cs" company="OpenAC .Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2014 - 2017 Projeto OpenAC .Net
//
//	 Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//	 The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//	 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;
using OpenAC.Net.CEP.Commom;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;

namespace OpenAC.Net.CEP.Webservice
{
    internal sealed class CorreiosWebservice : WebserviceCEP
    {
        #region Fields

        private const string CORREIOS_URL = "https://apps.correios.com.br/SigepMasterJPA/AtendeClienteService/AtendeCliente?wsdl";

        #endregion Fields

        #region Methods

        public override OpenLogradouro[] BuscarPorCEP(string cep)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(CORREIOS_URL);
                request.ProtocolVersion = HttpVersion.Version10;
                request.UserAgent = "Mozilla/4.0 (compatible; Synapse)";
                request.Method = "POST";

                var postData = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>" +
                               "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
                               "xmlns:cli=\"http://cliente.bean.master.sigep.bsb.correios.com.br/\"> " +
                               " <soapenv:Header/>" +
                               " <soapenv:Body>" +
                               " <cli:consultaCEP>" +
                               " <cep>" + cep.OnlyNumbers() + "</cep>" +
                               " </cli:consultaCEP>" +
                               " </soapenv:Body>" +
                               " </soapenv:Envelope>";

                var byteArray = Encoding.UTF8.GetBytes(postData);
                var dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                string retorno;

                // ReSharper disable once AssignNullToNotNullAttribute
                using (var stHtml = new StreamReader(request.GetResponse().GetResponseStream(), OpenEncoding.ISO88591))
                    retorno = stHtml.ReadToEnd();

                var doc = XDocument.Parse(retorno);
                var element = doc.ElementAnyNs("Envelope").ElementAnyNs("Body").ElementAnyNs("consultaCEPResponse").ElementAnyNs("return");

                var endereco = new OpenLogradouro
                {
                    CEP = element.ElementAnyNs("cep").GetValue<string>(),
                    Bairro = element.ElementAnyNs("bairro").GetValue<string>(),
                    Municipio = element.ElementAnyNs("cidade").GetValue<string>(),
                    Complemento = $"{element.ElementAnyNs("complemento").GetValue<string>()}{Environment.NewLine}{element.ElementAnyNs("complemento2").GetValue<string>()}",
                    Logradouro = element.ElementAnyNs("end").GetValue<string>(),
                    UF = (OpenUF)Enum.Parse(typeof(OpenUF), element.ElementAnyNs("uf").GetValue<string>())
                };

                endereco.TipoLogradouro = endereco.Logradouro.Split(' ')[0];
                endereco.Logradouro = endereco.Logradouro.SafeReplace(endereco.TipoLogradouro, string.Empty);

                return new[] { endereco };
            }
            catch (Exception exception)
            {
                throw new OpenException(exception, "Erro ao consulta CEP.");
            }
        }

        #endregion Methods
    }
}