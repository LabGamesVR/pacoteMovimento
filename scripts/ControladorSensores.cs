using UnityEngine;
using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class ControladorSensores : MonoBehaviour
{
    public Vector2 dados;
    private Vector2 thread_safe_dado;
    Mutex mutex = new Mutex();
    public MostradorStatusConexao mostrador;
    private string ultimoDispositivo;
    private Thread buscarDadosSerialThread;
    private bool buscarDadosSerialRunning = true;

    private UdpClient client;
    private Thread netThread;
    private bool isListening = false;
    private int port = 5555;

    private StatusConexao novoStatus = StatusConexao.Desconectado;
    public StatusConexao statusConexao = StatusConexao.Desconectado;

    void Start()
    {
        if (FindObjectsOfType<ControladorSensores>().Length > 1)
            Destroy(gameObject);
        else
        {
            DontDestroyOnLoad(gameObject);
            buscarDadosSerialThread = new Thread(BuscarDadosPorta);
            buscarDadosSerialThread.Start();

            netThread = new Thread(new ThreadStart(PortListener));
            netThread.IsBackground = true;
            netThread.Start();
        }
    }

    void Update()
    {
        if (mostrador && novoStatus != statusConexao)
        {
            statusConexao = novoStatus;
            mostrador.Mostrar(statusConexao);
        }
        mutex.WaitOne();
        dados = thread_safe_dado;
        mutex.ReleaseMutex();
    }
    private static bool IsValidArduinoResponse(string response)
    {
        string pattern = @".*-?\d+\.\d+\t-?\d+\.\d+.*";
        return new Regex(pattern).IsMatch(response);
    }

    bool InterpretarMensagem(string mensagem)
    {
        if (IsValidArduinoResponse(mensagem))
        {
            string[] f_reps = mensagem.Split('\t');
            string id = f_reps[0];
            try
            {
                Vector2 v = new Vector2(
                    float.Parse(f_reps[1]),
                    float.Parse(f_reps[2])
                );
                // A leitura é feita em radianos pelo sensor.
                // Como em Unity por padrão trabalhamos com graus,
                // aqui fazemos a conversão
                v *= 180f / Mathf.PI;

                mutex.WaitOne();
                thread_safe_dado = v;
                mutex.ReleaseMutex();

                if (ultimoDispositivo != id)
                    ultimoDispositivo = id;
                return true;
            }
            catch (Exception) { }
        }
        return false;
    }

    private SerialPort BuscarArduino()
    {
        while (buscarDadosSerialRunning)
        {
            bool erro = false;
            SerialPort newSerialPort = null;
            try
            {
                
                foreach (string port in SerialPort.GetPortNames())
                {
                    // Debug.Log("tentando na porta "+port);

                    erro = false;
                    try
                    {
                        newSerialPort = new SerialPort(port, 9600)
                        {
                            ReadTimeout = 500
                        };
                        newSerialPort.Open();
                    }
                    catch (Exception)
                    {
                        // Debug.Log(e);
                        erro = true;
                    }
                    if (!erro)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Thread.Sleep(100);
                            try
                            {
                                string response = newSerialPort.ReadLine();
                                if (IsValidArduinoResponse(response))
                                    return newSerialPort;
                            }
                            catch (Exception)
                            {
                                // Debug.Log(e);
                            }
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                //Isso existe para capturar e ignorar o IOException "Too many open files"
            }
        }
        return null;
    }

    private void BuscarDadosPorta()
    {
        SerialPort serialPort = null;
        while (buscarDadosSerialRunning)
        {
            if (serialPort == null)
            {
                if (mostrador != null &&  statusConexao == StatusConexao.Serial)
                    novoStatus = StatusConexao.Desconectado;

                serialPort = BuscarArduino();

                if (mostrador != null)
                    novoStatus = StatusConexao.Serial;
            }
            else
            {
                try
                {
                    string response = serialPort.ReadLine();
                    // print(response);
                    if (response == "")
                    {
                        serialPort = null;
                    }

                    InterpretarMensagem(response);
                }
                catch (Exception)
                {
                    // Debug.Log("ERRO: " + e);
                    serialPort = null;
                }
            }

            Thread.Sleep(100); // To avoid tight looping
        }
    }

    void PortListener()
    {
        try
        {
            client = new UdpClient(port);
            client.Client.ReceiveTimeout = 500;
            isListening = true;
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);

            while (isListening)
            {
                if (statusConexao == StatusConexao.Desconectado || statusConexao == StatusConexao.WiFi)
                {
                    try
                    {
                        byte[] receivedBytes = client.Receive(ref remoteEndPoint);

                        // Convert byte data to string and log the message
                        string receivedMessage = Encoding.ASCII.GetString(receivedBytes);
                        if (InterpretarMensagem(receivedMessage) && statusConexao == StatusConexao.Desconectado)
                            novoStatus = StatusConexao.WiFi;
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode == SocketError.TimedOut && statusConexao == StatusConexao.WiFi)
                            novoStatus = StatusConexao.Desconectado;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error: {e.Message}");
        }
    }
    public string ObterDispostivoAtual()
    {
        return ultimoDispositivo;
    }

    private void OnApplicationQuit()
    {
        isListening = false;
        client?.Close();
        netThread?.Abort();

        buscarDadosSerialRunning = false;
        if (buscarDadosSerialThread != null && buscarDadosSerialThread.IsAlive)
        {
            buscarDadosSerialThread.Join();
        }
    }
}
