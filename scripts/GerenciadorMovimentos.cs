using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;

public class GerenciadorMovimentos : MonoBehaviour
{
    #region basico

[System.Serializable]
    public struct Nomes
    {
        public string topName;
        public string botName;
        public string midName;
        public string leftName;
        public string rightName;
        public Nomes(string topName = "Cima", string botName = "Baixo", string midName = "Neutro", string leftName = "Esquerda", string rightName = "Direita")
        {
            this.topName = topName;
            this.botName = botName;
            this.midName = midName;
            this.leftName = leftName;
            this.rightName = rightName;
        }
    }
    [System.Serializable]
    public struct ItemDicionarioNomesPorDispositivo{
        public string dispositivo;
        public Nomes nomes;
    }

    public Nomes nomesPadrao;
    public ItemDicionarioNomesPorDispositivo[] dicionarioNomesPorDispositivo;

    Vector2 topRot, botRot, midRot, leftRot, rightRot;
    bool verticalAxis = true, horizontalAxis = true;
    public ControladorSensores sensor;
    Movimento2Eixos mov2;
    public bool pronto = false;
    public Movimento2Eixos.Analise analise;
    void Start()
    {
        sensor = FindAnyObjectByType<ControladorSensores>();
        mov2 = new();
    }

    public bool CalibrarDir(Movimento2Eixos.Direcao dir)
    {
        if (sensor.statusConexao != StatusConexao.Desconectado && sensor.dados != Vector2.zero)
        {
            switch (dir)
            {
                case Movimento2Eixos.Direcao.top:
                    topRot = sensor.dados;
                    break;
                case Movimento2Eixos.Direcao.bot:
                    botRot = sensor.dados;
                    break;
                case Movimento2Eixos.Direcao.left:
                    leftRot = sensor.dados;
                    break;
                case Movimento2Eixos.Direcao.right:
                    rightRot = sensor.dados;
                    break;
                case Movimento2Eixos.Direcao.mid:
                    midRot = sensor.dados;
                    break;
            }
            CheckIfShouldUpdateModel();
            return true;
        }
        return false;

    }
    void CheckIfShouldUpdateModel()
    {
        pronto = topRot != Vector2.zero &&
            botRot != Vector2.zero &&
            midRot != Vector2.zero &&
            leftRot != Vector2.zero &&
            rightRot != Vector2.zero;
        if (pronto)
        {
            mov2.Set(topRot, botRot, midRot, leftRot, rightRot);
        }
    }

    public Movimento2Eixos.Thresholds GetThresholds()
    {
        return mov2.GetThresholds();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (mov2 != null)
            mov2.DrawGizmos(transform.position);
    }
#endif
    #endregion

    public struct Registro
    {
        public float tempo;
        public float extremoAtingido;
        public float tempoParaNeutro;
        public Movimento2Eixos.Direcao mov;
        public Registro(float tempo, float extremoAtingido, float tempoParaNeutro, Movimento2Eixos.Direcao mov)
        {
            this.tempo = tempo; this.extremoAtingido = extremoAtingido; this.tempoParaNeutro = tempoParaNeutro; this.mov = mov;
        }
    }
    private float intervaloFacilitada = 0.1f;
    public float minimoTempoEmPosicao = 0.5f;
    float momentoInicioExercicio;
    float momentoUltimaFacilitada;
    private Queue<Movimento2Eixos.Direcao> movimentosASeremFeitos;
    private int movimentosRealizados;
    private UnityAction<int, float> callbackNeutro, callbackFimMovimento;
    bool jaFoiParaONeutro = false;
    float inicioValidadeExercicio;
    float extremoAtingido;

    string ondeSalvar;

    float TempoParaNeutro;
    List<Registro> Registros = new();

    FixedQueue<float> ultimasLeituras = new(5);

    void Update()
    {
        if (pronto)
        {
            Vector2 currentRot = FindObjectOfType<ControladorSensores>().dados;
            analise = mov2.GetEvaluation(currentRot);
            // se tem movimentos na lista
            if (movimentosASeremFeitos != null && movimentosASeremFeitos.Any())
            {
                Movimento2Eixos.Direcao desejado = jaFoiParaONeutro ? movimentosASeremFeitos.Peek() : Movimento2Eixos.Direcao.mid;
                Movimento2Eixos.Direcao atual = mov2.ObterDirecao(analise, true, desejado == Movimento2Eixos.Direcao.top || desejado == Movimento2Eixos.Direcao.bot);

                if (desejado == Movimento2Eixos.Direcao.mid)
                    ultimasLeituras.Clear();
                else
                {
                    ultimasLeituras.Enqueue(desejado == Movimento2Eixos.Direcao.top || desejado == Movimento2Eixos.Direcao.bot ? analise.progressoFrontal : analise.progressoLateral);

                    float leituraDeExtremo =
                        desejado == Movimento2Eixos.Direcao.top ? Mathf.Max(extremoAtingido, analise.progressoFrontal) :
                        desejado == Movimento2Eixos.Direcao.bot ? Mathf.Min(extremoAtingido, analise.progressoFrontal) :
                        desejado == Movimento2Eixos.Direcao.left ? Mathf.Min(extremoAtingido, analise.progressoLateral) :
                        desejado == Movimento2Eixos.Direcao.right ? Mathf.Max(extremoAtingido, analise.progressoLateral) :
                        0.5f;

                    if (Mathf.Abs(leituraDeExtremo - ultimasLeituras.Average()) < 0.1f)
                        extremoAtingido = leituraDeExtremo;
                }

                if (desejado == atual)
                {
                    if (inicioValidadeExercicio == 0f)
                        inicioValidadeExercicio = Time.time;
                    else
                    {
                        if (Time.time - inicioValidadeExercicio > minimoTempoEmPosicao)
                        {
                            if (desejado == Movimento2Eixos.Direcao.mid)
                            {
                                jaFoiParaONeutro = true;
                                TempoParaNeutro = Time.time - momentoInicioExercicio;
                                callbackNeutro(movimentosRealizados, TempoParaNeutro);
                            }
                            else
                            {
                                float tempo = Time.time - momentoInicioExercicio;
                                callbackFimMovimento(movimentosRealizados++, tempo);
                                momentoInicioExercicio = Time.time;
                                jaFoiParaONeutro = false;
                                Movimento2Eixos.Direcao mov = movimentosASeremFeitos.Dequeue();
                                mov2.Dificultar(mov, extremoAtingido);

                                if (ondeSalvar != "")
                                {
                                    Registro r = new Registro(tempo, extremoAtingido, TempoParaNeutro, mov);
                                    Registros.Add(r);
                                    if (!movimentosASeremFeitos.Any())
                                    {
                                        SalvarRegistro();
                                        Registros = new();
                                    }
                                }

                            }
                        }
                    }
                }
                else
                {
                    inicioValidadeExercicio = 0f;
                    if (Time.time - momentoUltimaFacilitada > intervaloFacilitada && desejado != Movimento2Eixos.Direcao.mid)
                    {
                        mov2.Facilitar(movimentosASeremFeitos.Peek());
                        momentoUltimaFacilitada = Time.time;
                    }
                }
            }
        }
    }
    public void SetSaveLocation(string local)
    {
        ondeSalvar = local;
    }

    public void IniciarSequenciaDeExercicios(Queue<Movimento2Eixos.Direcao> movimentos, UnityAction<int, float> AoIrParaONeutro, UnityAction<int, float> AoFinalizarMovimento)
    {
        Registros = new();
        movimentosASeremFeitos = movimentos;
        movimentosRealizados = 0;
        callbackFimMovimento = AoFinalizarMovimento;
        callbackNeutro = AoIrParaONeutro;
        jaFoiParaONeutro = false;
        inicioValidadeExercicio = 0f;
        momentoInicioExercicio = Time.time;
        momentoUltimaFacilitada = Time.time;
        extremoAtingido = 0.5f;
    }

    public string NomeMovimento(Movimento2Eixos.Direcao mov, string dispositivo)
    {
        foreach (ItemDicionarioNomesPorDispositivo item in dicionarioNomesPorDispositivo)
        {
            if(item.dispositivo == dispositivo){
                return mov switch
                {
                    Movimento2Eixos.Direcao.top => item.nomes.topName,
                    Movimento2Eixos.Direcao.bot => item.nomes.botName,
                    Movimento2Eixos.Direcao.left => item.nomes.leftName,
                    Movimento2Eixos.Direcao.right => item.nomes.rightName,
                    _ => item.nomes.midName,
                };
            }
        }
        return mov switch
        {
            Movimento2Eixos.Direcao.top => nomesPadrao.topName,
            Movimento2Eixos.Direcao.bot => nomesPadrao.botName,
            Movimento2Eixos.Direcao.left => nomesPadrao.leftName,
            Movimento2Eixos.Direcao.right => nomesPadrao.rightName,
            _ => nomesPadrao.midName,
        };
    }

    private void SalvarRegistro()
    {
        if (ondeSalvar != "")
        {
            //garante que pasta exista
            string dir = System.IO.Path.GetDirectoryName(ondeSalvar);
            System.IO.Directory.CreateDirectory(dir);

            // se arquivo nao existir, cria com header
            if (!System.IO.File.Exists(ondeSalvar))
                System.IO.File.WriteAllText(ondeSalvar, "Eixo de movimento;Movimento;Tempo para neutro;Tempo para completar;Angulo atingido");

            string dispositivo = sensor.ObterDispostivoAtual();
            string outTxt = System.DateTime.Now.ToString("\nyyyy-dd-MM:HH:mm;" + dispositivo);
            foreach (Registro item in Registros)
            {
                outTxt += "\n" + (item.mov == Movimento2Eixos.Direcao.top || item.mov == Movimento2Eixos.Direcao.bot ? "Frontal" : "Lateral")
                    + ";" + NomeMovimento(item.mov, dispositivo)
                    + ";" + item.tempoParaNeutro.ToString("0.00")
                    + ";" + item.tempo.ToString("0.00")
                    + ";" + mov2.ObterAngulo(item.mov, item.extremoAtingido).ToString("0.0") + "°"
                ;
            }

            System.IO.File.AppendAllText(ondeSalvar, outTxt);
        }
    }
    public Movimento2Eixos.Direcao ObterDirecao()
    {
        return mov2.ObterDirecao(analise);
    }
}
