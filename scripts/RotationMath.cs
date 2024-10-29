using System;
using UnityEngine;
public static class RotationMath
{
    public static Vector3 rot2pos(Vector2 rot){
        return Quaternion.Euler(rot) * new Vector3(0f, 0f, 0.5f);
    }

    public static Vector3 closestInPlane(Vector3 pos, Vector3 normal){
        return pos - Vector3.Dot(pos, normal)*normal;
    }

    //quanto é preciso rodar P1 ao longo do eixo para chegar em P2, clockwise
    public static float ComputeRotationAngle(Vector3 P1, Vector3 P2, Vector3 axis)
    {
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

    public static float CalcularProgresso(Vector2 minRot, Vector2 midRot, Vector2 maxRot, Vector2 query){
        Vector3 maxPos = rot2pos(maxRot);
        Vector3 midPos = rot2pos(midRot);
        Vector3 minPos = rot2pos(minRot);
        Vector3 testePos = rot2pos(query);

        //cria um plano contendo os pontos medios entre o ponto máximo e medio e o ponto mimimo e médio
        Vector3 normal = Vector3.Cross((maxPos+midPos)*0.5f, (minPos+midPos)*0.5f).normalized;

        //projeta os pontos para este plano
        Vector3 maxProjection = closestInPlane(maxPos,normal).normalized*0.5f;
        Vector3 midProjection = closestInPlane(midPos,normal).normalized*0.5f;
        Vector3 minProjection = closestInPlane(minPos,normal).normalized*0.5f;
        Vector3 testeProjection = closestInPlane(testePos,normal).normalized*0.5f;
        
        //cria uma normal que sempre aponta para o mesmo lado em relação aos tres pontos dados
        Vector3 outraNormal = new Plane(maxProjection, midProjection, minProjection).normal;

        float anguloMax = ComputeRotationAngle(maxProjection, midProjection, outraNormal);
        if(anguloMax < 0) anguloMax += 360;
        float anguloMin = ComputeRotationAngle(minProjection, midProjection, outraNormal);
        if(anguloMin > 0) anguloMin -= 360;
        float anguloTeste = ComputeRotationAngle(testeProjection, midProjection, outraNormal);
        
        float anguloLimite = (anguloMax + anguloMin)*0.5f + 180f;
        if(anguloLimite>0 && anguloTeste<0) anguloTeste+=360f;
        if(anguloLimite<0 && anguloTeste>0) anguloTeste-=360f;

        return 0.5f+ (anguloTeste>anguloLimite
            ? (360f-anguloTeste) / anguloMin 
            :anguloTeste / anguloMax
            )*0.5f;

    }
}
