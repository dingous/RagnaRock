# Relatório da entrega — 21/09/2026

## Resultado

Projeto Unity RagnaRock, versão de desenvolvimento **0.1.0**, enviado a **dingous/RagnaRock**, nas branches `master` e `main`. A visibilidade pública escolhida pelo proprietário foi mantida. O projeto contém implementação em C#, cena de entrada, metadados, shaders, campanha, testes e ferramentas. Não há executável Unity incluído nem autorização para considerar esta entrega homologada para produção.

## Implementado em código

Câmera 3D superior oblíqua; quatro integrantes fixos; ataque automático com quatro instrumentos; prioridade por clique; coleta de itens; XP e escolhas de melhorias; evolução visual; ondas progressivas; 18 atos/54 ondas; encontros de chefe; vida do palco; fúria/Muralha de Som; vitória/derrota; menus; opções; movimento reduzido; checkpoint versionado com checksum/backup; referências culturais nominais e cenográficas; meshes/sons originais produzidos proceduralmente.

## Executado na preparação local

- Inspeção dos arquivos e revisão manual de fluxos, limites de pools e persistência.
- Verificação estrutural automatizada de arquivos/JSON/XML, referências GUID, metadados únicos, vinculação da cena, referências de assemblies, nomes de componentes MonoBehaviour, delimitadores e um padrão de erro C# conhecido.
- Correção do fluxo de draft esgotado, isolamento de saves de teste, uma declaração `var` múltipla no harness e o identificador esperado do último ato em testes.
- Inicialização e verificação de integridade do repositório Git local; empacotamento ZIP e Git bundle.

O resultado detalhado do validador está em `structural-validation.json`. **Esse PASS não é compilação C#, execução dos 21 testes Unity nem homologação do jogo.**

## Executado no GitHub após o envio

- Importação dos 87 arquivos originais no commit `0d8c0d11ecf652a1acab6ab9da521a70b491af9e`.
- Conferência da árvore Git remota: `16d2a09864a9994440fcba01bff00f12504c5288`, idêntica à árvore do pacote original.
- Workflow **Source integrity and core rules**, execução **35635548831**, job **106451964559**: resultado **success**. Verificação estrutural e compilação/execução do harness C# em .NET 8 aprovadas.

O harness valida as regras independentes do motor. Não valida APIs Unity, cena, shaders, áudio, interface ou build do jogo. O relatório `structural-validation.json` preserva a execução estrutural da entrega inicial; sua lista de atividades não executadas pertence àquela execução, não ao resultado posterior do GitHub.

## Preparado, mas ainda NÃO validado
- 15 testes EditMode e 6 PlayMode. Unity Editor/licença não estavam disponíveis neste ambiente.
- Importação dos pacotes e da cena no Unity; compilação dos shaders; inspeção visual/sonora no motor.
- Build Windows, teste fora do Editor, performance, memória, acessibilidade, balanceamento e campanha completa.
- Workflow Unity: é manual e exige runner Windows licenciado; não foi executado.

## GitHub — envio concluído

O usuário criou `dingous/RagnaRock`, e a integração autenticada enviou o conteúdo completo. As branches `master` e `main` receberam o mesmo commit, sem force-push. A criação inicial pela assistência tinha sido bloqueada por login; essa pendência foi resolvida pela criação feita pelo proprietário.

`Tools/Publish-GitHub.ps1` permanece como ferramenta da entrega inicial e não deve ser executado para este repositório já criado. O `.bundle` entregue preserva o histórico local original. A importação no GitHub preserva o conteúdo, mas possui seu próprio histórico de commits.

## Limitações de conteúdo e produção

Arte low-poly e animação procedural são uma implementação inicial, não arte final validada. Os 18 encontros compartilham uma base de chefe; não há 18 personagens de chefe exclusivos. A trilha é sintetizada e não uma produção de estúdio/licenciada. A cobertura dos estilos é uma curadoria de 18 atos, não todas as vertentes existentes. A história e as referências precisam de revisão editorial integral.

O nome solicitado conflita potencialmente com um nome comercial já em uso: existe o jogo *Ragnarock*. Não houve pesquisa de registro de marca ou liberação jurídica. O arquivo `RELEASE_CHECKLIST.md` registra os bloqueios que devem ser resolvidos antes de publicar.

**Estado de release: BLOQUEADO — validação no Unity, playtesting, revisão de nome/conteúdo e distribuição pendentes.**
