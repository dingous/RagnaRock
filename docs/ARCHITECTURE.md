# Arquitetura

`RagnaBootstrap` inicia uma única `GameSession`. A cena serializa só esse componente; todo o restante é determinístico na construção e criado em runtime. As classes MonoBehaviour têm arquivos com o mesmo nome e metadados estáveis. Não há dependência de projetos anteriores nem de `Importados.PrefabObjeto`.

## Assemblies

`RagnaRock.Core` não referencia Unity: enumerações, configuração validável, curva das ondas, RNG com estado, estatísticas, draft e checkpoint. `RagnaRock.Runtime` referencia Core e uGUI. `RagnaRock.Editor` existe apenas no Editor. Testes ficam em assemblies próprios, não no player normal.

## Estado e tempo

Um relógio da sessão recebe `unscaledDeltaTime` limitado a 50ms por atualização. A simulação avança apenas nos estados apropriados; menus de upgrade/pausa congelam inimigos, projéteis e timers. `Time.timeScale` não é alterado. Em travamentos longos o jogo desacelera em vez de executar um pico de ataques de recuperação. Áudio usa relógio próprio e é pausado conforme estado/foco.

## Combate e pools

`EnemyWorld` faz movimento radial e usa uma lista limitada de ativos. Mortes são marcadas e removidas após a iteração; divisão/invocação não invalidam o enumerador. `WeaponSystem` usa uma lista temporária pré-alocada para perfuração. `EffectPool` evita roubar um aviso perigoso para exibir efeitos cosméticos. Pools: 96 inimigos ativos, 24 morteiros, 240 itens, 112 efeitos e 40 textos de dano. O pool de cada tipo de inimigo cresce até seu pico observado, portanto o número total de objetos inativos não é igual ao limite de ativos.

## Persistência

PlayerPrefs guarda JSON versionado com SHA-256 e slot de backup. O hash detecta corrupção acidental; não é criptografia nem mecanismo anti-cheat. A retomada representa o início da onda, com estado do gerador aleatório. Preferências, recordes e partida usam chaves separadas. Testes injetam namespace aleatório para não tocar nas chaves reais do jogador. PlayerPrefs pode ser removido pelo sistema/usuário; não há backup em nuvem.

## Arte e som

`MeshCraft` gera triângulos com normais e cores por vértice. `StageArt` monta músicos, instrumentos e arquitetura, com shaders próprios no Built-in Render Pipeline. Materiais e meshes são compartilhados entre os objetos reutilizados e descartados ao destruir a sessão. `MetalAudio` produz amostras de áudio originais em blocos, sem IO de rede, e faz transição entre dois AudioSources de música.

## Automatização

O workflow Source checks verifica integridade e compila/executa Core em .NET. O workflow Unity é manual e requer runner Windows licenciado, com label `ragnarock-unity`; não dispara código de PRs em máquina própria. Nenhum workflow publica em loja. Nesta entrega, nenhum workflow remoto foi executado.
