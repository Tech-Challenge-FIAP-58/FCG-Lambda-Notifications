using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Newtonsoft.Json;
using System.Net;
using System.Reflection.Metadata;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FCG.Lambda.Notifications;


/*
    * Foi criado um email de teste só pra fazer o envio e recebimento, para confirmar que está funionando
    * Este email foi configurado no AWS SES (Simple Email Service)
    * Ele foi criado e encontra-se em Sandbox. Por este motivo ele só consegue enviar email para ele mesmo.
    * Desta forma qualquer email destinatário que a função receba, vamos descartar o destinatário e sermpre enviar para o email abaixo.
    * ** IMPORTANTE ** os emãils estão caindo na caixa SPAM
    * 
    * 
    * email: fcgpostech@gmail.com
    * senha: TgbYhn(61
    * 
    * Estamos disponibilizando a senha para que logem no gmail e confirmem que os emails estão chegando.
    *     * 
    * JSON exemplo para teste no MOCK
      {\"destinatario\":\"clienteteste@teste.com\",\"assunto\":\"Teste de envio pela AWS Lambda\",\"corpo\":\"Olá! Este é um e-mail de teste enviado pela função Lambda para validação do trabalho acadêmico.\"}
    * 
    * FCGNotificationsApi
    * https://bbn5c427p4.execute-api.us-east-2.amazonaws.com/default/FCGNotifications
    * JSON exemplo para teste no Postman
       {
          "destinatario": "clienteteste@teste.com",
          "assunto": "Teste de envio pela AWS Lambda",
          "corpo": "Olá! Este é um e-mail de teste enviado por uma função Lambda para validação do trabalho acadêmico."
        }
    * 
    * 
    */

public class Function
{
    private readonly IAmazonSimpleEmailService _sesClient = new AmazonSimpleEmailServiceClient();

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        try
        {
            var dados = JsonConvert.DeserializeObject<emailAEnviar>(request.Body);

            var emailRequest = new SendEmailRequest
            {
                Source = "fcgpostech@gmail.com", // verificado no SES
                Destination = new Destination
                {
                    // conta em sandbox → enviando para e-mail verificado
                    ToAddresses = new List<string> { "fcgpostech@gmail.com" }
                },
                Message = new Message
                {
                    Subject = new Content(dados.assunto),
                    Body = new Body
                    {
                        Text = new Content(
                            "Destinatário original: " + dados.destinatario +
                            "\n\n" + dados.corpo)
                    }
                }
            };

            await _sesClient.SendEmailAsync(emailRequest);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "Email enviado com sucesso"
            };
        }
        catch (Exception ex)
        {
            // log simples para CloudWatch
            context.Logger.LogLine("Erro ao enviar e-mail: " + ex.Message + " stacktrace: " + ex.StackTrace);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = "Falha ao enviar e-mail " + ex.Message + " stacktrace: " + ex.StackTrace
            };
        }
    }
}

public class emailAEnviar
{
    public string destinatario { get; set; }
    public string assunto { get; set; }
    public string corpo { get; set; }
}
