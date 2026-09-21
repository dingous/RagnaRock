# RagnaRock — Defenda o Último Palco

**Projeto Unity 3D original, versão de desenvolvimento 0.1.0. Não homologado para produção.**

Um survivor de defesa de palco: quatro integrantes de uma banda permanecem no centro da arena e atacam hordas automaticamente. Você decide as melhorias, prioriza ameaças e usa uma habilidade de emergência. A campanha percorre 18 atos temáticos, em 54 ondas, das raízes do metal ao brutal death.

## Abrir e jogar

1. Instale pelo Unity Hub o **Unity 6000.3.12f1** e o módulo **Windows Build Support**. O projeto usa **Built-in Render Pipeline**, não URP/HDRP.
2. Extraia este projeto e use **Add project from disk** no Hub, apontando para a pasta que contém `Assets`, `Packages` e `ProjectSettings`.
3. Aguarde a importação dos pacotes. Abra `Assets/RagnaRock/Scenes/RagnaRock.unity` — ou use **RagnaRock > Abrir jogo**.
4. Pressione **Play** e **COMEÇAR UM NOVO SHOW**.

A cena tem somente o bootstrap salvo: músicos, ambiente, iluminação, interface, pools e trilha são construídos em runtime. Não se trata de prefabs faltando. Para ver tudo no Editor, entre em Play. Não copie as pastas Library/Temp de outro projeto.

### Controles

| Ação | Controle |
|---|---|
| Atacar / coletar itens | Automático; a banda não se movimenta |
| Priorizar um inimigo | Clique sobre ele; prioridade dura até 8 segundos |
| Acelerar atração de itens próximos | Passe o cursor sobre eles |
| Muralha de Som | Espaço ou botão; exige 100% de fúria |
| Escolher melhoria | Clique ou 1, 2, 3 |
| Iniciar onda antes da contagem | Enter ou botão |
| Pausar / retornar | Esc; P também pausa durante combate |
| Arquivo do metal | F1 ou menu |

**Alvo inicial:** Windows x64, teclado e mouse, paisagem. Há tratamento de toque e safe area, mas isso não constitui uma versão Android/iOS validada. Não há multiplayer.

## Banda e combate

| Integrante | Instrumento | Mecânica |
|---|---|---|
| Nyx | Voz | Cone sônico; na evolução final, alcance angular de 360° |
| Raven | Guitarra | Riff perfurante; ignora blindagem e acrescenta explosão no nível 8 |
| Atlas | Baixo | Onda de pressão, lentidão e repulsão |
| Knox | Bateria | Bombas em arco; duas no nível 4 e três no nível 8 |

Todos começam no nível 1. Cada instrumento vai até 8, com mudanças visuais nos níveis 4 e 8. Oito equipamentos passivos complementam dano, frequência, alcance, defesa, vida, coleta, crítico e recuperação. XP abre escolhas sem opções repetidas; chefes derrubam núcleos que evoluem um instrumento. Itens são atraídos automaticamente para o palco.

A vida pertence ao palco/banda. Quando chega a zero, a partida termina. A Muralha de Som causa dano em área, repulsa inimigos e protege por 3,5 segundos. Chefes anunciam ataques antes do impacto. As primeiras ondas têm poucos inimigos; quantidade, resistência e cadência crescem com limites explícitos.

## Conteúdo incluído

18 atos, 54 ondas e 18 encontros de chefe, com oito arquétipos de inimigo. Os chefes compartilham uma base visual e de comportamento com escalonamento: não são 18 modelos exclusivos. Cenografias temáticas e variações de cor/decoração acompanham a campanha. Os quatro músicos, instrumentos, criaturas, palco e efeitos usam geometria original em C#.

A trilha é **sintetizada proceduralmente**, com loops originais por ato e variações de BPM, timbre, afinação e percussão. Não contém músicas comerciais, covers, gravações ou samples de bandas. É uma trilha de desenvolvimento; mixagem profissional e direção musical final ainda exigem aprovação.

Referências nominais a bandas aparecem em inscrições e no Arquivo do Metal. Duas alusões cenográficas explícitas são **Iron Man**, do Black Sabbath, e **Master of Puppets**, do Metallica, sem letras, capas ou logotipos copiados. A rota é uma curadoria ampla, não uma enciclopédia completa de todos os subgêneros nem uma linha histórica exata.

Menu, opções de áudio, redução de movimento, números de dano opcionais, pausa ao perder foco, recordes e checkpoint com checksum/backup estão incluídos. O checkpoint retoma **o início da onda**, não o frame exato. Morrer ou vencer encerra o checkpoint; recordes permanecem.

## Validação e build

Verificação estrutural, sem Unity:

```powershell
python .\Tools\validate_project.py
dotnet run --project .\Tests\CoreHarness\CoreHarness.csproj --configuration Release
```

O primeiro comando verifica arquivos, JSON, metadados, referências e estrutura lexical; **não compila C#**. O segundo compila e executa as regras C# reais com .NET 8, mas **não valida APIs ou recursos do Unity**.

No Unity, execute **RagnaRock > Validar projeto** e abra **Window > General > Test Runner** para EditMode e PlayMode. Alternativamente, com o Editor fechado:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Run-UnityChecks.ps1 -Build
```

O caminho padrão do Editor está no script; use `-UnityPath` para outro local. O build fica em `Builds/Windows/RagnaRock.exe`. O jogo não é enviado a lojas nem publicado automaticamente. Leia [o checklist de release](docs/RELEASE_CHECKLIST.md) e [o relatório de entrega](docs/DELIVERY_STATUS.md).

## GitHub

O pacote inclui código e um histórico Git local separado em `RagnaRock.bundle`. **A criação remota não foi concluída nesta entrega:** o conector reconheceu a conta `dingous`, mas não oferece criação de repositório, e a sessão do navegador pediu login.

Para criar **dingous/RagnaRock privado** e enviar `master`, instale Git e GitHub CLI, revise o script e execute na raiz extraída:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Publish-GitHub.ps1
```

O script autentica pelo fluxo do GitHub CLI, confirma a conta `dingous`, recusa repositório remoto já existente e nunca usa force-push. Não precisa colocar senha ou token em arquivos. Uma falha não equivale a criação bem-sucedida; o script imprime a confirmação do GitHub somente ao terminar.

Para restaurar o Git local a partir do bundle, use `git clone RagnaRock.bundle RagnaRock` em uma pasta vazia. A restauração cria um remote local; remova somente esse remote local antes de usar o publicador, após conferir `git remote -v`.

## Estrutura

- `Assets/RagnaRock/Scripts/Core`: regras independentes do motor.
- `Assets/RagnaRock/Scripts/Runtime`: sessão, combate, modelos, áudio, UI e persistência.
- `Assets/RagnaRock/Scripts/Editor`: validação e build.
- `Assets/RagnaRock/Resources`: campanha e shaders incluídos no player.
- `Assets/RagnaRock/Tests`: testes EditMode e PlayMode, com saves isolados.
- `Tests/CoreHarness`: execução das regras reais sem Unity.
- `Tools`: integridade, teste/build local e criação do repositório.
- `docs`: projeto, referências, procedência e critérios de lançamento.

## Nome e publicação

**RagnaRock é o nome de trabalho solicitado.** Já existe um jogo comercial chamado *Ragnarock*. Esta entrega não atesta disponibilidade de nome/marca e não afirma relação com esse jogo. Revise nome, classificação indicativa, materiais de divulgação, termos das dependências e conteúdo cultural antes de distribuir comercialmente.

Não foram adicionados analytics, anúncios, pagamentos, SDKs de loja, coleta de dados ou contas online. O repositório não concede uma licença pública de código aberto; veja [a nota de procedência](docs/ASSET_PROVENANCE.md).
