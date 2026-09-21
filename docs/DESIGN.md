# Documento de jogo — RagnaRock 0.1.0

## Identidade

Survivor 3D com câmera ortográfica oblíqua: uma fortaleza de vinil cercada por uma arena circular. A banda funciona como uma unidade de quatro armas. A ausência de deslocamento é intencional. A decisão do jogador está na construção de equipamentos, prioridade de alvo e timing defensivo da Muralha de Som.

Objetivo da partida: sobreviver às 54 ondas e ao encontro final. Primeiro ato: raízes/proto-metal. Último: brutal death com alusões a slam/grindcore. Os ramos intermediários incluem heavy, doom/stoner/sludge, NWOBHM, speed, thrash/crossover, power, progressive, gothic/symphonic, folk/viking, black, groove, industrial/alternative/nu, metalcore/deathcore, death, melodic death e technical death. Esses agrupamentos são uma decisão de escopo, não uma taxonomia única do metal.

## Ciclo

Menu → preparação → onda → itens/XP → melhoria pausada → retomada → fim da onda → preparação. A cada três ondas há chefe. Fim de campanha: vitória; vida do palco zero: derrota. Confirmação é exigida antes de substituir um checkpoint ou abandonar uma onda para o menu.

Ao subir de nível são sorteadas até três melhorias elegíveis e distintas. Escolhas acumuladas são preservadas. Equipamentos no máximo saem do sorteio; se todos estiverem no máximo, as escolhas excedentes viram reparo e a partida continua. Núcleos de chefe priorizam um dos instrumentos de menor nível.

## Curva e limites

Onda 1: dez inimigos comuns, sem chefe. A cota cresce até 132, mas o máximo simultâneo é 96. Inimigos adicionais de divisão/invocação têm orçamento de 28 por onda. Os ataques não dependem de colisores, física, NavMesh ou animações importadas. Ondas terminam pela eliminação real da cota e dos inimigos adicionais, não por cronômetro que deixa ameaças para trás.

A matemática exata fica em `GameRules.cs`; a identidade dos atos fica em `Campaign.json`. A curva precisa de playtesting humano antes de ser chamada de balanceada. Os nomes e identidades de chefe variam; há uma base compartilhada, com escalonamento e invocações em atos posteriores.

## Leitura visual

Quatro cores identificam funções, mas nomes e instrumentos também distinguem os músicos. Guitarra: linha; vocal: cone/anel; baixo: anel baixo; bateria: projétil em arco. Golpes perigosos possuem aviso textual e anel de antecipação. O modo de movimento reduzido desativa trepidação e movimentos decorativos fortes; avisos de ataques continuam visíveis.

## Limites do conteúdo de lançamento

Esta entrega não inclui campanha narrativa com cutscenes, artistas licenciados, gravações de estúdio, 18 chefes modelados separadamente, todas as vertentes existentes, localização além de PT-BR, remapeamento completo de controles, certificação de acessibilidade, multiplayer, integração de loja, instalador assinado, Android/iOS publicado ou validação de desempenho. Não há promessa de FPS sem medição.
