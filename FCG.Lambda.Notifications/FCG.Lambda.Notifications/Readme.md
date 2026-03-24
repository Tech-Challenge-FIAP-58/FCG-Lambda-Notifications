# FCG Lambda Notifications

Função AWS Lambda em `.NET 8` responsável por consumir mensagens de um evento de fila (`MQEvent`), decodificar o payload em Base64, desserializar os dados de e-mail e enviar notificações via `Amazon SES`.

## Visão geral do funcionamento

O fluxo principal está no arquivo `Function.cs`, no método `FunctionHandler(MQEvent input, ILambdaContext context)`.

Resumo do processamento:

1. Valida se o evento possui mensagens (`input.messages`).
2. Para cada mensagem:
   - Decodifica `message.data` de Base64 para texto JSON.
   - Desserializa o JSON para `EmailWrapper`.
   - Extrai `wrapper.Message` com os dados do e-mail.
   - Monta um `SendEmailRequest` do SES.
   - Envia o e-mail com `_sesClient.SendEmailAsync(...)`.
3. Registra logs de sucesso e erro no `CloudWatch Logs` via `context.Logger`.

## Estrutura do projeto

- `Function.cs`
  - Implementa o handler da Lambda.
  - Contém os modelos de entrada:
    - `MQEvent`
    - `MQMessage`
    - `EmailWrapper`
    - `EmailMessage`
- `FCG.Lambda.Notifications.csproj`
  - Define target `net8.0`.
  - Dependências AWS e JSON.
- `aws-lambda-tools-defaults.json`
  - Configurações padrão de deploy e runtime para AWS Lambda.

## Tecnologias e pacotes

- `.NET 8`
- `Amazon.Lambda.Core`
- `Amazon.Lambda.Serialization.SystemTextJson`
- `AWSSDK.SimpleEmail`
- `Newtonsoft.Json`

## Contrato de entrada esperado

O handler espera um objeto com a estrutura:

- `messages`: lista de mensagens
- cada mensagem contém:
  - `data`: string Base64 contendo JSON

### Exemplo de evento recebido pela Lambda

```json
{
  "messages": [
    {
      "data": "eyJNZXNzYWdlIjp7IkRlc3RpbmF0YXJpbyI6ImNsaWVudGVAZXhhbXBsZS5jb20iLCJBc3N1bnRvIjoiUGVkaWRvIGNvbmZpcm1hZG8iLCJDb3JwbyI6IlNlbyBwZWRpZG8gZm9pIHJlY2ViaWRvIGNvbSBzdWNlc3NvLiJ9fQ=="
    }
  ]
}
```

O Base64 acima representa o JSON:

```json
{
  "Message": {
    "Destinatario": "cliente@example.com",
    "Assunto": "Pedido confirmado",
    "Corpo": "Seu pedido foi recebido com sucesso."
  }
}
```

## Formato do e-mail enviado

O envio é feito com `SendEmailRequest` do SES usando:

- `Source`: `fcgpostech@gmail.com`
- `ToAddresses`: `fcgpostech@gmail.com`
- `Subject`: valor de `Message.Assunto`
- `Body.Text`: 
  - inclui o destinatário original (`Message.Destinatario`)
  - inclui o conteúdo (`Message.Corpo`)

> Observação: atualmente o e-mail sempre é enviado para `fcgpostech@gmail.com` (destino fixo), apenas informando no corpo quem era o destinatário original.

## Logs e tratamento de erros

- Se não houver mensagens: loga `Nenhuma mensagem recebida`.
- Se falhar a desserialização: loga `Erro ao desserializar mensagem` e segue para a próxima mensagem.
- Se ocorrer exceção no processamento/envio: loga `Erro ao processar mensagem: ...`.
- Em sucesso: loga `Email enviado com sucesso`.

Isso garante processamento por mensagem, sem interromper todo o lote ao falhar uma entrada específica.

## Pré-requisitos para execução/deploy

1. Conta AWS com permissões para:
   - `lambda:*` (deploy/execução conforme papel utilizado)
   - `ses:SendEmail`
   - `logs:CreateLogGroup`, `logs:CreateLogStream`, `logs:PutLogEvents`
2. E-mail remetente verificado no Amazon SES (`fcgpostech@gmail.com`).
3. Se a conta estiver em sandbox SES, destinatários também precisam ser verificados.
4. .NET SDK 8 instalado.
5. Ferramenta de deploy:

```bash
dotnet tool install -g Amazon.Lambda.Tools
```

## Build local

Na raiz do projeto (`FCG.Lambda.Notifications`):

```bash
dotnet restore
dotnet build -c Release
```

## Deploy via CLI

Com `aws-lambda-tools-defaults.json` configurado, execute na raiz do projeto:

```bash
dotnet lambda deploy-function
```

Configurações relevantes já definidas:

- `function-name`: `FCGNotifications`
- `function-runtime`: `dotnet8`
- `function-handler`: `FCG.Lambda.Notifications::FCG.Lambda.Notifications.Function::FunctionHandler`
- `region`: `us-east-2`
- `function-memory-size`: `512`
- `function-timeout`: `30`

## Pontos de atenção

- `IAmazonSimpleEmailService` é instanciado diretamente no código (`new AmazonSimpleEmailServiceClient()`), usando credenciais padrão do ambiente Lambda.
- O projeto usa `System.Text.Json` para serialização da Lambda (atributo assembly), mas usa `Newtonsoft.Json` para desserializar o payload interno.
- As classes de modelo (`MQEvent`, `MQMessage`, etc.) usam nomes de propriedade em minúsculo/maiúsculo conforme o contrato esperado (`messages`, `data`, `Message`, `Destinatario`, `Assunto`, `Corpo`).

## Handler da função

O entrypoint configurado é:

`FCG.Lambda.Notifications::FCG.Lambda.Notifications.Function::FunctionHandler`

Esse é o método que a AWS invoca ao receber eventos vinculados à função Lambda.
