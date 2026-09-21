# Porta de saída para produção

Status inicial: **BLOQUEADO / não homologado**. Marcar uma caixa exige evidência correspondente; a existência do código não equivale a um teste aprovado.

## Importação e execução

- [ ] Importação limpa no Unity 6000.3.12f1, sem Library anterior, pacotes resolvidos e nenhum erro no Console.
- [ ] `RagnaRock > Validar projeto` aprovado, shaders sem erro e arquivos com referências válidas.
- [ ] EditMode e PlayMode aprovados; guardar XMLs e logs em uma entrega versionada.
- [ ] Build Windows x64 concluído e executado fora do Editor, em máquina limpa.
- [ ] Abrir/fechar/reabrir a aplicação sem exceções, cena preta, fonte ausente ou material rosa.

## Partida e regressões

- [ ] Jogar a campanha inteira, do ato 1 ao 18, sem atalhos que escondam travamentos.
- [ ] Validar legibilidade, dano, equilíbrio e variedade de cada ato e chefe com jogadores.
- [ ] Testar escolha consecutiva de melhorias, todos os equipamentos no máximo e núcleos adicionais.
- [ ] Testar vitória, derrota, reinício e retorno ao menu em cada estado.
- [ ] Testar pausa, Alt-Tab, mudança de foco, áudio silenciado e movimento reduzido.
- [ ] Encerrar durante uma onda e confirmar retomada do início dela; corromper slot em ambiente de teste e recuperar backup.
- [ ] Testar teclado/mouse e cliques sobre UI sem acionar alvos por trás.

## Visual, desempenho e áudio

- [ ] Inspecionar 1280×720, 1920×1080, 2560×1440, ultrawide e escala do Windows; nenhum texto cortado.
- [ ] Medir frame time, memória, GC, draw calls e picos de áudio nos atos finais; registrar hardware e qualidade.
- [ ] Fazer teste prolongado/reinícios repetidos para verificar crescimento de meshes, materiais, UI e AudioClips.
- [ ] Aprovar modelagem, animação procedural, efeitos, iluminação, mixagem e transições musicais.
- [ ] Testar master/música/efeitos/mute com caixas e fones; validar ausência de clipping e estalos perceptíveis.

## Distribuição e conteúdo

- [ ] Definir nome comercial após revisão do nome de trabalho RagnaRock/Ragnarock; não existe liberação de marca nesta entrega.
- [ ] Revisar menções a bandas, títulos e texto histórico; não sugerir participação ou endosso.
- [ ] Revisar condições de Unity/pacotes e todos os assets que forem adicionados posteriormente.
- [ ] Preparar classificação indicativa, ícones, screenshots reais, descrição, página de suporte e política aplicável.
- [ ] Remover a identificação de desenvolvimento somente após aprovação; atualizar versão e changelog.
- [ ] Produzir pacote/instalador e assinatura exigidos pelo canal de distribuição; testar atualização/desinstalação.
- [ ] Guardar commit/tag, logs, hash do build, aprovação de QA e plano de correção/rollback.

Android, iOS, WebGL e gamepad completo precisam de testes/plataformas próprios; não estão certificados pelo alvo Windows.
