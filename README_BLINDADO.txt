=== Pacote Blindado (Unity) ===
Regras de estabilidade (conforme combinado):
  1) TMP Essentials preservado em Assets/TextMesh Pro/Resources (SDF + TMP Settings.asset)
  2) Todos os .meta necessários presentes
  3) Packages/manifest.json e packages-lock.json normalizados (versões fixas)
  4) Scenes/YAMLs íntegros (sem alterações destrutivas)
  5) Sem pasta Library/ no ZIP

Blindagem adicionada:
  • Nome curto na raiz: Jogo/
  • Verificação de integridade: INTEGRITY/content_manifest.sha256 + verify_content_hashes.cmd
  • Limpeza segura antes de abrir: CleanLibrary.cmd
  • Abertura pelo Unity Hub: OpenProject_6000.cmd (abre via Hub; selecione 6000.2.9f1 no Hub)

Como usar (Windows):
  1) Clique direito no ZIP baixado → Propriedades → marque "Desbloquear" → Aplicar.
  2) Extraia o ZIP com 7-Zip/WinRAR para um caminho curto, por ex.: C:\Dev\Jogo
  3) (Opcional) Dê duplo clique em INTEGRITY\verify_content_hashes.cmd — tudo deve ficar "OK".
  4) Dê duplo clique em CleanLibrary.cmd (remove Library/Temp/obj/Logs locais se existirem).
  5) Abra com OpenProject_6000.cmd (Unity Hub) ou a versão 6000.2.9f1 instalada.

Observação: O arquivo de hash é da ÁRVORE DE CONTEÚDO (Assets/Packages/ProjectSettings/UserSettings e scripts de blindagem),
não do ZIP em si (pois o hash do ZIP muda se o próprio arquivo de hash estiver dentro dele).
