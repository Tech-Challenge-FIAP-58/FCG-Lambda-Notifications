using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FCG.Lambda.Notifications;

public class Function
{
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        if (sqsEvent?.Records == null || sqsEvent.Records.Count == 0)
        {
            context.Logger.LogLine("Nenhuma mensagem recebida");
            return;
        }

        foreach (var record in sqsEvent.Records)
        {
            try
            {
                context.Logger.LogLine("Mensagem recebida: " + record.Body);

                var wrapper = JsonConvert.DeserializeObject<EmailWrapper>(record.Body);
                var dados = wrapper?.Message;

                if (dados == null)
                {
                    context.Logger.LogLine("Erro ao desserializar mensagem");
                    continue;
                }

                // SES indisponível no AWS Academy — simulando envio via log
                context.Logger.LogLine("[SIMULADO] Email enviado com sucesso");
                context.Logger.LogLine($"[SIMULADO] Para     : {dados.Destinatario}");
                context.Logger.LogLine($"[SIMULADO] Assunto  : {dados.Assunto}");
                context.Logger.LogLine($"[SIMULADO] Corpo    : {dados.Corpo}");

                await Task.CompletedTask;
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
