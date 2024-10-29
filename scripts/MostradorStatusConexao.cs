using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Image))]
public class MostradorStatusConexao : MonoBehaviour
{
    public float rotationSpeed = 90f;
    public Sprite imageProcura;
    public Sprite imageConectadoSerial;
    public Sprite imageConectadoWifi;
    private UnityEngine.UI.Image img;
    private bool rotating = true;
    void Start()
    {
        img = GetComponent<UnityEngine.UI.Image>();
        Mostrar(StatusConexao.Desconectado);
    }
    void Update(){
        if(rotating){
            Vector3 r = transform.rotation.eulerAngles;
            r.z-=Time.deltaTime * rotationSpeed;
            transform.rotation = Quaternion.Euler(r);
        }
    }

    public void Mostrar(StatusConexao status)
    {
        switch (status)
        {
            case StatusConexao.Serial:
                img.sprite = imageConectadoSerial;
                rotating = false;
                transform.rotation = Quaternion.identity;
                break;
            case StatusConexao.WiFi:
                img.sprite = imageConectadoWifi;
                rotating = false;
                transform.rotation = Quaternion.identity;
                break;
            default:
                img.sprite = imageProcura;
                rotating = true;
                break;
        }
    }
}
