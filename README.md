# AutoInsight API QA & Compliance

---

## Integrantes

| Nome                         | RM       | Turma   | E-mail                 | GitHub                                         | LinkedIn                                   |
|------------------------------|----------|---------|------------------------|------------------------------------------------|--------------------------------------------|
| Arthur Vieira Mariano        | RM554742 | 2TDSPF  | arthvm@proton.me       | [@arthvm](https://github.com/arthvm)           | [arthvm](https://linkedin.com/in/arthvm/)  |
| Guilherme Henrique Maggiorini| RM554745 | 2TDSPF  | guimaggiorini@gmail.com| [@guimaggiorini](https://github.com/guimaggiorini) | [guimaggiorini](https://linkedin.com/in/guimaggiorini/) |
| Ian Rossato Braga            | RM554989 | 2TDSPY  | ian007953@gmail.com    | [@iannrb](https://github.com/iannrb)           | [ianrossato](https://linkedin.com/in/ianrossato/)      |

---

## Estrutura do Repositório

| Pasta | Conteúdo |
|--------|-----------|
| `/api` | Código-fonte da API em .NET |
| `/tests` | Testes automatizados via Postman |

---

## Execução da API (.NET)

1. Clone o repositório:  

   ```bash
   git clone https://github.com/autoinsight-labs/qa.git
   cd qa/api
   ```

2. Configure o banco (PostgreSQL local ou Docker):

   ```bash
   docker compose up -d
   ```

3. Execute as migrations:

   ```bash
   dotnet ef database update
   ```

4. Rode a API:

   ```bash
   dotnet run
   ```

5. URL padrão:

   ```
   http://localhost:5232
   ```

---

## Parte A — Testes Manuais (Azure Boards)

🔗 [Clique aqui para abrir o plano de testes](https://dev.azure.com/AutoInsightLabs/Ping/_testPlans/define?planId=441&suiteId=442)

---

## Parte B — Testes Automatizados (Postman)

**Collection:**

* `AutoInsight-QA.postman_collection.json`

### Testes Automatizados Incluídos

Cenário                     | Endpoint                               | Validação                              |
--------------------------- | -------------------------------------- | -------------------------------------- |
Criar pátio                 | `POST /v2/yards`                       | Criação e armazenamento do `yardId`    |
Convite (criar + aceitar)   | `/v2/yards/{yardId}/invites`           | Status `Pending` → `Accepted`          |
Veículo (criar + finalizar) | `/v2/yards/{yardId}/vehicles`          | Status `Finished`, `leftAt` preenchido |
Forecast                    | `/v2/yards/{yardId}/capacity-forecast` | Retorna pontos com `occupancyRatio`    |

---

### Execução no Postman

1. Importe a collection (`AutoInsight-QA.postman_collection.json`).

2. Crie um Environment no Postman com as seguintes variáveis:

   | Nome      | Valor                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
   | --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
   | `baseUrl` | `http://localhost:5232`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
   | `jwt`     | `eyJhbGciOiJSUzI1NiIsImtpZCI6IjU0NTEzMjA5OWFkNmJmNjEzODJiNmI0Y2RlOWEyZGZlZDhjYjMwZjAiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL3NlY3VyZXRva2VuLmdvb2dsZS5jb20vYXV0b2luc2lnaHQtNmYxZjkiLCJhdWQiOiJhdXRvaW5zaWdodC02ZjFmOSIsImF1dGhfdGltZSI6MTc2MjI3NzMwMiwidXNlcl9pZCI6IldQNlptNFBGUjFTWktoV3JsMko3VVFKVlNrdDIiLCJzdWIiOiJXUDZabTRQRlIxU1pLaFdybDJKN1VRSlZTa3QyIiwiaWF0IjoxNzYyMjc3MzAyLCJleHAiOjE3NjIyODA5MDIsImVtYWlsIjoidGVzdGVndWkyQGdtYWlsLmNvbSIsImVtYWlsX3ZlcmlmaWVkIjpmYWxzZSwiZmlyZWJhc2UiOnsiaWRlbnRpdGllcyI6eyJlbWFpbCI6WyJ0ZXN0ZWd1aTJAZ21haWwuY29tIl19LCJzaWduX2luX3Byb3ZpZGVyIjoicGFzc3dvcmQifX0.gH1ndt58JeYue6zUs8lYdRHKo1MD7r30h2o-BFIx3wDM5SHGOv5e2lGIBR1YhFG0VZJ0NX8gZGQPPwjl_1LB6Mk1jC4ruxmNd7syRsHfK8OLJ17MB7FaKd1HYhvm_Uvng1Z7UvZsaNl528V8yElVrxuFZ8lcKNspqYkBiShoE4HGKDM86FmaZ1Bx6CTBbR8d5eZdZ2TysQlqOZswkm7kqvJxBFZjTjSZ8vvwf6BzHH7pvxvU8h7H8C5AXnlyrxwCLljNdI-Jpx7IUPybG0JoRXIVb67z0Q3Cg6bCOFmjoOJ5Er-uhAjlUiZKLOB1P_xS4-JW3b4psVXHCHjdD7VAOw` |

3. Na authorization da collection, adicione o header:

   ```
   Authorization: Bearer {{jwt}}
   ```

4. Execute a collection pelo Runner.

5. Todos os testes devem retornar 200 ou 201 e salvar automaticamente variáveis como `yardId`, `inviteId` e `vehicleId` durante a execução.

---

## Vídeo de demonstração

📺 [Clique aqui para abrir o vídeo de demonstração](https://youtu.be/i5Uk11JnYrU)

---

## Licença

Este projeto foi desenvolvido para fins acadêmicos como parte do challenge da Mottu FIAP.