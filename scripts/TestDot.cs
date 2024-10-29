using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TestDot : MonoBehaviour
{
    public Vector2 maxRot;
    public Vector2 midRot;
    public Vector2 minRot;
    public Vector2 testeRot;

    public Vector3 maxPos;
    public Vector3 midPos;
    public Vector3 minPos;
    public Vector3 testePos;

    public Vector3 maxProjection;
    public Vector3 midProjection;
    public Vector3 minProjection;
    public Vector3 testeProjection;

    public float anguloMax;
    public float anguloMid;
    public float anguloMin;
    public float anguloTeste;
    public float anguloLimite;
    public float progresso;

    private ControladorSensores sensores;

    public bool buttom;
    public string acao = "max";

    void Start()
    {
        sensores = FindObjectOfType<ControladorSensores>();
    }
    void Update()
    {
        if (sensores)
            testeRot = Vector2.Lerp(testeRot, sensores.dados, 1f);
    }

    Vector3 rot2pos(Vector2 rot)
    {
        return Quaternion.Euler(rot) * new Vector3(0f, 0f, 0.5f);
    }


    public Vector2 PositionToRotation(Vector3 position)
    {
        // Normalize the position to ensure it's on the unit sphere
        position.Normalize();

        // Calculate the yaw (rotation around the y-axis)
        float yaw = Mathf.Atan2(position.x, position.z) * Mathf.Rad2Deg;

        // Calculate the pitch (rotation around the x-axis)
        float pitch = Mathf.Asin(position.y) * Mathf.Rad2Deg;

        return new Vector2(pitch, yaw);
    }
    public static bool IsPointAbovePlane(Vector3 point, Vector3 planePosition, Quaternion planeRotation)
    {
        // Calculate the normal of the plane from its rotation
        Vector3 planeNormal = planeRotation * Vector3.up;

        // Calculate the vector from the plane position to the point
        Vector3 pointToPlane = point - planePosition;

        // Calculate the dot product of the plane normal and the point to plane vector
        float dotProduct = Vector3.Dot(pointToPlane, planeNormal);

        // If the dot product is greater than 0, the point is above the plane
        return dotProduct > 0;
    }
    private float radius = 0.5f;
    void DrawGizmosPlane2(Plane p, float planeSize = 1f)
    {
        Quaternion rotation = Quaternion.LookRotation(transform.TransformDirection(p.normal));
        Matrix4x4 trs = Matrix4x4.TRS(transform.TransformPoint(transform.position), rotation, Vector3.one);
        Gizmos.matrix = trs;
        // Gizmos.DrawCube(Vector3.zero, new Vector3(planeSize, planeSize, 0.0001f));
        Vector3 lastPoint = Vector3.zero;

        for (int i = 0; i <= 360; i += 10)
        {
            float angle = i * Mathf.Deg2Rad;
            Vector3 newPoint = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

            if (i > 0)
            {
                Gizmos.DrawLine(lastPoint, newPoint);
                Gizmos.DrawLine(transform.position, newPoint);
            }

            lastPoint = newPoint;
        }
        Gizmos.matrix = Matrix4x4.identity;
    }

    Vector3 closestInPlane(Vector3 pos, Vector3 normal)
    {
        return pos - Vector3.Dot(pos, normal) * normal;
    }

    public static float ComputeRotationAngle(Vector3 P1, Vector3 P2, Vector3 axis)
    {
        // Ensure the axis is a unit vector
        axis.Normalize();

        // Project the points onto the plane perpendicular to the rotation axis
        Vector3 P1_perp = P1 - Vector3.Dot(P1, axis) * axis;
        Vector3 P2_perp = P2 - Vector3.Dot(P2, axis) * axis;

        // Normalize the projected vectors
        P1_perp.Normalize();
        P2_perp.Normalize();

        // Compute the angle between the projected points
        float cosTheta = Vector3.Dot(P1_perp, P2_perp);
        float sinTheta = Vector3.Cross(P1_perp, P2_perp).magnitude;

        float angle = Mathf.Atan2(sinTheta, cosTheta) * Mathf.Rad2Deg;

        // Determine the direction of rotation
        if (Vector3.Dot(axis, Vector3.Cross(P1_perp, P2_perp)) < 0)
        {
            angle = -angle; // Clockwise rotation
        }

        return angle;
    }

    void tentativa2()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, .5f);

        maxPos = rot2pos(maxRot);
        midPos = rot2pos(midRot);
        minPos = rot2pos(minRot);
        testePos = rot2pos(testeRot);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + maxPos, 0.03f);
        Gizmos.DrawWireSphere(transform.position + minPos, 0.03f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + midPos, 0.03f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + testePos, 0.03f);

        Vector3 normal = Vector3.Cross((maxPos + midPos) * 0.5f, (minPos + midPos) * 0.5f).normalized;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + normal);

        Plane p = new Plane(normal, 0f);
        Gizmos.color = Color.magenta;
        DrawGizmosPlane2(p);

        maxProjection = closestInPlane(maxPos, normal).normalized * 0.5f;
        midProjection = closestInPlane(midPos, normal).normalized * 0.5f;
        minProjection = closestInPlane(minPos, normal).normalized * 0.5f;
        testeProjection = closestInPlane(testePos, normal).normalized * 0.5f;

        Vector3 outraNormal = new Plane(maxProjection, midProjection, minProjection).normal;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + outraNormal);


        anguloMax = ComputeRotationAngle(maxProjection, midProjection, outraNormal);
        if (anguloMax < 0) anguloMax += 360;
        anguloMin = ComputeRotationAngle(minProjection, midProjection, outraNormal);
        if (anguloMin > 0) anguloMin -= 360;
        anguloTeste = ComputeRotationAngle(testeProjection, midProjection, outraNormal);

        anguloLimite = (anguloMax + anguloMin) * 0.5f + 180f;
        if (anguloLimite > 0 && anguloTeste < 0) anguloTeste += 360f;
        if (anguloLimite < 0 && anguloTeste > 0) anguloTeste -= 360f;

        progresso = 0.5f + (anguloTeste > anguloLimite
            ? (360f - anguloTeste) / anguloMin
            : anguloTeste / anguloMax
            ) * 0.5f;

        Vector3 posLimite = closestInPlane(Quaternion.AngleAxis(-anguloLimite, outraNormal) * new Vector3(0f, 0f, 0.5f), normal).normalized * 0.5f;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(posLimite, 0.03f);


        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(maxProjection, 0.01f);
        Gizmos.DrawWireSphere(midProjection, 0.01f);
        Gizmos.DrawWireSphere(minProjection, 0.01f);
        Gizmos.DrawWireSphere(testeProjection, 0.01f);


    }

    void OnDrawGizmos()
    {
        if (buttom)
        {
            buttom = false;
            if (acao == "max")
            {
                maxRot = testeRot;
                acao = "mid";
            }
            else if (acao == "mid")
            {
                midRot = testeRot;
                acao = "min";
            }
            else if (acao == "min")
            {
                minRot = testeRot;
                acao = "max";
            }
        }
        tentativa2();
    }
}
