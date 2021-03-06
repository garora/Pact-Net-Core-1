﻿using System;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using PactNet.Configuration.Json;

namespace PactNet.Mocks.MockHttpService.Models
{
    public class HttpBodyContent
    {
        private bool _contentIsBase64Encoded;

        public dynamic Body { get; private set; }

        public string Content { get; private set; }

        public byte[] ContentBytes
        {
            get
            {
                if (Content == null)
                {
                    return null;
                }

                return _contentIsBase64Encoded ?
                    Convert.FromBase64String(Content) :
                    Encoding.GetBytes(Content);
            }
        }

        public MediaTypeHeaderValue ContentType { get; }

        public Encoding Encoding { get; }

        public HttpBodyContent(MediaTypeHeaderValue contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            if (contentType.CharSet == null)
            {
                throw new InvalidOperationException($"{nameof(contentType.CharSet)} must be supplied");
            }

            ContentType = contentType;
            Encoding = Encoding.GetEncoding(contentType.CharSet);
        }


        public void GenerateContent(dynamic body)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            if (IsJsonContentType())
            {
                string c = JsonConvert.SerializeObject(body, JsonConfig.ApiSerializerSettings);
                Content = c;
                Body = body;
            }
            else if (IsBinaryContentType())
            {
                if (body is byte[])
                {
                    Content = Convert.ToBase64String(body);
                    Body = body;
                    _contentIsBase64Encoded = true;
                }
                else //It's a string coming from json serialised content
                {
                    Content = Encoding.GetString(Convert.FromBase64String(body));
                    Body = body;
                }
            }
            else
            {
                if (body is byte[])
                {
                    Content = Encoding.ASCII.GetString(body);
                    Body = body;
                }
                else
                {
                    Type bodyType = body.GetType();
                    if (bodyType.Name == "RequestStream")
                    {
                        int length = (int)body.Length; 
                        byte[] data = new byte[length];
                        body.Read(data, 0, length);
                        Content = Encoding.ASCII.GetString(data);
                        Body = Content;
                        return;
                    }

                    if (body is string)
                    {
                        string stringContent = body;
                        Content = stringContent;
                        Body = stringContent;
                        return;
                    }

                    throw new ArgumentException("Dynamic body is of unknown type");
                }
            }
        }


        public HttpBodyContent(byte[] content, MediaTypeHeaderValue contentType) : this(contentType)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (IsJsonContentType())
            {
                var jsonContent = Encoding.GetString(content);
                Content = jsonContent;
                Body = JsonConvert.DeserializeObject<dynamic>(jsonContent);
            }
            else if (IsBinaryContentType())
            {
                Content = Encoding.GetString(content);
                Body = Convert.ToBase64String(content);
            }
            else
            {
                var stringContent = Encoding.GetString(content);
                Content = stringContent;
                Body = stringContent;
            }
        }

        private bool IsJsonContentType()
        {
            return ContentType.MediaType.IndexOf("application/", StringComparison.OrdinalIgnoreCase) == 0 &&
                ContentType.MediaType.IndexOf("json", StringComparison.OrdinalIgnoreCase) > 0;
        }

        private bool IsBinaryContentType()
        {
            return ContentType.MediaType.IndexOf("application/", StringComparison.OrdinalIgnoreCase) == 0 &&
                ContentType.MediaType.IndexOf("octet-stream", StringComparison.OrdinalIgnoreCase) > 0;
        }
    }
}