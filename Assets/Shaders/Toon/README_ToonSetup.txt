Saint Seiya Awakening - Toon Setup (Unity 2022.3 LTS + URP)
=========================================================

1. Ative o Universal Render Pipeline
   - Abra Window > Package Manager e instale "Universal RP".
   - Vá em Edit > Project Settings > Graphics e aponte o campo Scriptable Render Pipeline para um asset URP (crie um em Create > Rendering > URP Asset > Pipeline Asset).
   - Em Player > Other Settings altere Color Space para Linear.

2. Ajuste a qualidade do projeto
   - No asset URP, deixe HDR ligado, MSAA 4x (se couber na performance) e sombras com 2 cascatas.
   - No Forward Renderer, adicione as Renderer Features SSAO, Bloom e DepthNormals (utile para outlines Sobel caso queira testar).

3. Configure pós-processamento
   - Crie um Global Volume na cena com um profile contendo:
     * Tonemapping: ACES
     * Bloom: Threshold 1.15, Intensity 0.5
     * Color Adjustments: Saturation +0.08 (opcional)

4. Gere assets prontos para o shader
   - Use o menu Tools > Toon Setup > Create Default Ramps & MatCap para criar:
     * Assets/Shaders/Toon/Ramps/ramp_skin.png
     * Assets/Shaders/Toon/Ramps/ramp_armor.png
     * Assets/Shaders/Toon/MatCaps/matcap_gold.png

5. Material principal do personagem
   - Crie um Material usando o shader Toon/CharacterURP.
   - Aplique o Base Map do personagem e ajuste:
     * RampTex: use ramp_skin na pele ou ramp_armor em peças metálicas.
     * _RampBias ~ 0.05 (corrige o ponto de corte da sombra).
     * _SpecIntensity 0.5–0.7 para metais; 0.25 para pele.
     * _RimIntensity 0.25 e _RimPower 2.2 para halo suave.
     * _MatcapTex: matcap_gold (ajuda a dar brilho curto na armadura).

6. Outline
   - Adicione um segundo material Toon/Outline no SkinnedMeshRenderer.
   - Ajuste _Thickness entre 0.003 e 0.008 (varie conforme a escala do personagem).
   - A cor pode ser preta (#020208) ou azul-escuro para combinara com o cenário.

7. Iluminação rápida para testes
   - Uma Directional Light com intensidade 1.2, cor levemente quente.
   - Ative sombras macias, Bias 0.05 e Normal Bias 0.4.
   - Adicione Reflection Probes antes de posicionar os personagens para o MatCap e especular ficarem coerentes.

8. UI e VFX (referência rápida)
   - Use TextMesh Pro com materiais dourados/azuis.
   - Partículas com texturas estilizadas (gradientes e faíscas) + Shaders Additive.
   - Câmera com FOV 40 e Cinemachine para movimentos dramáticos em ultimates.

Aplicando os passos acima você alcança o look "anime estilizado" próximo ao Saint Seiya Awakening sem plugins externos. Ajuste RampTex e intensidade para cada personagem até que luz, sombra e brilhos reproduzam as referências fornecidas.
