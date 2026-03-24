using Amazon.Lambda.Core;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Newtonsoft.Json;
using System.Text;

// Serializer
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FCG.Lambda.Notifications;

public class Function
{
    private readonly IAmazonSimpleEmailService _sesClient = new AmazonSimpleEmailServiceClient();

    public async Task FunctionHandler(MQEvent input, ILambdaContext context)
    {
        if (input?.messages == null)
        {
            context.Logger.LogLine("Nenhuma mensagem recebida");
            return;
        }

        foreach (var message in input.messages)
        {
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(message.data));

                context.Logger.LogLine("Mensagem recebida: " + json);

                var wrapper = JsonConvert.DeserializeObject<EmailWrapper>(json);
                var dados = wrapper?.Message;

                if (dados == null)
                {
                    context.Logger.LogLine("Erro ao desserializar mensagem");
                    continue;
                }

                var emailRequest = new SendEmailRequest
                {
                    Source = "fcgpostech@gmail.com",
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { "fcgpostech@gmail.com" }
                    },
                    Message = new Message
                    {
                        Subject = new Content(dados.Assunto),
                        Body = new Body
                        {
                            Text = new Content(
                                $"Destinatário original: {dados.Destinatario}\n\n{dados.Corpo}")
                        }
                    }
                };

                await _sesClient.SendEmailAsync(emailRequest);

                context.Logger.LogLine("Email enviado com sucesso");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine("Erro ao processar mensagem: " + ex.Message);
            }
        }
    } 
}

public class EmailWrapper
{
    public EmailMessage Message { get; set; }
}

public class EmailMessage
{
    public string Destinatario { get; set; }
    public string Assunto { get; set; }
    public string Corpo { get; set; }
}
public class MQEvent
{
    public List<MQMessage> messages { get; set; }
}

public class MQMessage
{
    public string data { get; set; }
}
