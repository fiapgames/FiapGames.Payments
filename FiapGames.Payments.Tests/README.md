# FiapGames.Payments.Tests

Testes unitários do Payments, rodados pelo passo **Build & Test** da pipeline (`dotnet test FiapGames.Payments.sln`).

**Esta é só a base**, criada para a pipeline da Fase 4 ter o que executar. O banco é o provider InMemory do EF Core, então os testes não precisam de Postgres.

## Coberto

- `PaymentService`: o evento de pagamento mantém pedido, usuário e jogo, e o pagamento gravado tem o mesmo resultado do evento

## Pendente

Para as classes com dependências, use mocks. Sugestão: NSubstitute.

- [ ] `PaymentService`: tornar a aprovação sorteada injetável (hoje usa `Random.Shared`) para testar aprovado e rejeitado de forma determinística
- [ ] `OrderPlacedConsumer`: processa o pedido, publica o `PaymentProcessedEvent` e chama as notificações
- [ ] `NotificationsClient`: falha na Azure Function vira `LogWarning` e não derruba o processamento
- [ ] Pedido repetido: o índice único em `OrderId` impede cobrar duas vezes
