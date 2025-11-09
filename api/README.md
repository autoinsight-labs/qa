# AutoInsight API

## 🚀 Sobre o Projeto

A **AutoInsight API** é uma API RESTful construída com ASP.NET Core (.NET 9.0) para orquestrar o fluxo operacional de pátios, equipes e veículos de entrega. O projeto consolida recursos para administrar pátios, convidar colaboradores, acompanhar o status da frota e gerar previsões de capacidade em tempo real, tudo versionado sob o prefixo `/v2`.

## 🧰 Tecnologias Principais

- **ASP.NET Core Minimal APIs (net9.0)** – endpoints enxutos e performáticos.
- **Entity Framework Core + Npgsql** – persistência relacional em PostgreSQL com suporte a migrações.
- **FluentValidation** – validação declarativa dos contratos de entrada.
- **Scalar / OpenAPI** – documentação interativa da API.
- **xUnit + WebApplicationFactory** – suíte de testes unitários e de integração.
- **Serviços de domínio** – *YardCapacitySnapshotService* e *YardCapacityForecastService* para métricas e previsões.

## 🏛️ Decisões Arquiteturais

- **Minimal APIs em vez de Controllers**: as operações são simples e orientadas a recursos; usar Minimal APIs reduz *boilerplate*, melhora o *throughput* e facilita o versionamento por grupos (`MapGroup("/v2")`).
- **Organização por “feature folders”**: cada domínio (yards, vehicles, employees, invites) possui subpastas com `Endpoints`, `Request` e `Response`, mantendo o código próximo ao contrato que expõe e diminuindo o acoplamento.
- **Contratos imutáveis com records**: os DTOs usam `record` para representar contratos de entrada/saída. Isso evita *over-posting*, preserva o domínio (`AutoInsight.Models`) e simplifica a serialização.
- **Validação explícita com FluentValidation**: cada endpoint encapsula suas regras de negócio em validadores dedicados, garantindo mensagens consistentes e facilitando testes.
- **Autenticação desacoplada**: o *middleware* `AuthenticatedUserMiddleware` extrai o usuário das *claims* e injeta um `AuthenticatedUser` na pipeline, permitindo que cada endpoint escolha quando exigir autenticação.
- **Sem HATEOAS por pragmatismo**: a API atende *frontends* web/mobile internos que já conhecem os fluxos. Links hipermídia aumentariam o tamanho das respostas, complicariam o cache e não agregariam valor imediato. Em vez disso, centralizamos a descoberta de recursos na documentação OpenAPI publicada automaticamente e na UI do Scalar, que fornecem esquema completo, exemplos executáveis e navegação entre rotas. Optamos por respostas enxutas com parâmetros de cursor e status textual.
- **Serviços assíncronos de capacidade**: a pasta `services/` e `ml/` encapsulam lógica de snapshot e forecast para manter o domínio principal limpo e permitir evolução independente de algoritmos de previsão.

## ⚙️ Configuração Rápida

1. Instale o **.NET SDK 9.0**.
2. Configure a *connection string* `DefaultConnection` para PostgreSQL (ver `appsettings*.json`).
3. Restaure dependências e aplique migrações:
   ```bash
   dotnet restore
   dotnet ef database update
   ```
4. Execute a API:
   ```bash
   dotnet run --project aspnet.csproj
   ```
5. A documentação interativa fica disponível em `http://localhost:5100/scalar` (modo desenvolvimento).

## 🧪 Como executar os testes

Os testes utilizam EF Core InMemory e não exigem dependências externas. Após restaurar as dependências:

```bash
cd aspnet_v2
dotnet test aspnet.csproj
```

- Use `--filter` para rodar cenários específicos (`dotnet test aspnet.csproj --filter YardEndpoints`).
- O relatório de cobertura pode ser habilitado com `-p:CollectCoverage=true` (Coverlet já está referenciado no projeto).

## 📚 Rotas

Todas as rotas estão versionadas sob o prefixo `/v2`.

### 🏢 Yards

| Método | Endpoint | Descrição | Autenticação |
| ------ | -------- | --------- | ------------ |
| POST   | `/v2/yards` | Cria um novo pátio e registra o usuário autenticado como admin inicial. | Sim |
| GET    | `/v2/yards` | Lista pátios com paginação via `cursor` e `limit`. | Não |
| GET    | `/v2/yards/{yardId}` | Recupera detalhes do pátio, funcionários e convites pendentes. | Não |
| PATCH  | `/v2/yards/{yardId}` | Atualiza nome, capacidade ou proprietário (restrito ao admin/owner). | Sim |
| DELETE | `/v2/yards/{yardId}` | Remove um pátio, desde que o usuário autenticado seja o proprietário. | Sim |
| GET    | `/v2/yards/{yardId}/capacity-forecast` | Gera previsão horária de ocupação (parâmetro opcional `horizonHours`). | Não |

### 🏍️ Vehicles

| Método | Endpoint | Descrição | Parâmetros Notáveis | Autenticação |
| ------ | -------- | --------- | ------------------- | ------------ |
| POST   | `/v2/yards/{yardId}/vehicles` | Registra um veículo no pátio e vincula um beacon exclusivo (UUID/Major/Minor); `assigneeId` opcional deve pertencer ao mesmo pátio. | Body: `plate`, `model`, `beacon.uuid`, `beacon.major`, `beacon.minor`, `assigneeId`. | Não |
| GET    | `/v2/yards/{yardId}/vehicles` | Lista veículos com paginação (`cursor`, `limit`) e filtro (`filter=active|departed|all`), sempre retornando o beacon associado. | Query: `cursor`, `limit`, `filter`. | Não |
| GET    | `/v2/yards/{yardId}/vehicles/{vehicleId}` | Retorna detalhes, incluindo status, responsável atual (quando houver) e beacon (UUID/Major/Minor). | Path: `vehicleId`. | Não |
| PATCH  | `/v2/yards/{yardId}/vehicles/{vehicleId}` | Atualiza status, responsável e/ou beacon, controlando transições e snapshots. | Body: `status`, `assigneeId`, `beacon.uuid`, `beacon.major`, `beacon.minor`. | Não |

### 👥 Yard Employees

| Método | Endpoint | Descrição | Autenticação |
| ------ | -------- | --------- | ------------ |
| GET    | `/v2/yards/{yardId}/employees` | Lista colaboradores do pátio com paginação por cursor. | Não |
| GET    | `/v2/yards/{yardId}/employees/{employeeId}` | Consulta dados de um colaborador específico. | Não |
| PATCH  | `/v2/yards/{yardId}/employees/{employeeId}` | Atualiza nome, foto ou cargo (admins controlam papéis, usuário pode editar próprio perfil). | Sim |
| DELETE | `/v2/yards/{yardId}/employees/{employeeId}` | Remove colaborador; apenas admins do pátio podem executar. | Sim |

### ✉️ Employee Invites

| Método | Endpoint | Descrição | Autenticação |
| ------ | -------- | --------- | ------------ |
| POST   | `/v2/yards/{yardId}/invites` | Cria convites para o pátio; exige que o solicitante seja admin do pátio. | Sim |
| GET    | `/v2/yards/{yardId}/invites` | Lista convites do pátio com paginação (`cursor`, `limit`). | Não |
| GET    | `/v2/invites/{inviteId}` | Exibe detalhes de um convite, incluindo informações do pátio. | Não |
| DELETE | `/v2/invites/{inviteId}` | Cancela um convite; apenas admins do pátio podem remover. | Sim |
| POST   | `/v2/invites/{inviteId}/accept` | Aceita convite pendente, cria o funcionário e marca data de aceite. | Sim |
| POST   | `/v2/invites/{inviteId}/reject` | Rejeita convite pendente. | Sim |
| GET    | `/v2/invites/user` | Lista convites associados ao e-mail do usuário autenticado. | Sim |

> **Observação:** parâmetros de paginação aceitam `limit` (1–100) e `cursor` (UUID). As respostas paginadas seguem o contrato `PagedResponse` com `data`, `pageInfo` e `count`.

## 👥 Equipe de Desenvolvimento

| Nome                        | RM      | Turma    | E-mail                 | GitHub                                         | LinkedIn                                   |
|-----------------------------|---------|----------|------------------------|------------------------------------------------|--------------------------------------------|
| Arthur Vieira Mariano       | RM554742| 2TDSPF   | arthvm@proton.me       | [@arthvm](https://github.com/arthvm)           | [arthvm](https://linkedin.com/in/arthvm/)  |
| Guilherme Henrique Maggiorini| RM554745| 2TDSPF  | guimaggiorini@gmail.com| [@guimaggiorini](https://github.com/guimaggiorini) | [guimaggiorini](https://linkedin.com/in/guimaggiorini/) |
| Ian Rossato Braga           | RM554989| 2TDSPY   | ian007953@gmail.com    | [@iannrb](https://github.com/iannrb)           | [ianrossato](https://linkedin.com/in/ianrossato/)      |

## 📄 Licença

Projeto desenvolvido para o challenge FIAP ✕ Mottu. Uso acadêmico e experimental.
