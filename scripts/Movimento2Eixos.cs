using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Movimento2Eixos
{

    public struct Analise
    {
        public float ProbabilidadeEixoLateral;
        public float progressoLateral;
        public float progressoFrontal;
    }
    [System.Serializable]
    public struct Thresholds
    {
        public float top, bot, left, right;
        public Thresholds(float top, float bot, float left, float right)
        {
            Debug.Log("Criando novo th com top " + top);
            this.top = top;
            this.bot = bot;
            this.left = left;
            this.right = right;
        }

        public static Thresholds Default => new(0.8f, 0.2f, 0.2f, 0.8f);
    }

    public enum Direcao
    {
        top, bot, left, right, mid
    }

    const float THRESHOLD_SCALING = 0.002f;
    const float MAX_THRESHOLD_SCALING = 0.1f;

    // Informacoes usadas para calcular o progresso, configuradas e deixadas constantes
    // (exceto se recalibrar)
    Vector2 topRot, botRot, midRot, leftRot, rightRot;
    Vector3 frontNormal, sideNormal,
        midFrontProjection, midSideProjection, currentFrontProjection, currentSideProjection;
    float frontAnguloLimite, sideAnguloLimite;
    float topAngle, botAngle, leftAngle, rightAngle;


    public Thresholds thresholds;

    public void Set(Vector2 topRot, Vector2 botRot, Vector2 midRot, Vector2 leftRot, Vector2 rightRot)
    {
        Debug.Log("set");
        thresholds = Thresholds.Default;

        this.topRot = topRot;
        this.botRot = botRot;
        this.midRot = midRot;
        this.leftRot = leftRot;
        this.rightRot = rightRot;

        Vector3 topPos = RotationMath.rot2pos(topRot);
        Vector3 BotPos = RotationMath.rot2pos(botRot);
        Vector3 midPos = RotationMath.rot2pos(midRot);
        Vector3 leftPos = RotationMath.rot2pos(leftRot);
        Vector3 rightPos = RotationMath.rot2pos(rightRot);

        frontNormal = Vector3.Cross((topPos + midPos) * 0.5f, (BotPos + midPos) * 0.5f).normalized;
        sideNormal = Vector3.Cross((leftPos + midPos) * 0.5f, (rightPos + midPos) * 0.5f).normalized;

        Vector3 topProjection = RotationMath.closestInPlane(topPos, frontNormal).normalized * 0.5f;
        Vector3 botProjection = RotationMath.closestInPlane(BotPos, frontNormal).normalized * 0.5f;
        midFrontProjection = RotationMath.closestInPlane(midPos, frontNormal).normalized * 0.5f;

        Vector3 leftProjection = RotationMath.closestInPlane(leftPos, sideNormal).normalized * 0.5f;
        Vector3 rightProjection = RotationMath.closestInPlane(rightPos, sideNormal).normalized * 0.5f;
        midSideProjection = RotationMath.closestInPlane(midPos, sideNormal).normalized * 0.5f;

        // Dependendo dos angulos, as normais podem apontar para um extremo ou outro.
        // Usando esse metodo, obtem normais com direção consistente
        frontNormal = Vector3.Cross(botProjection - midFrontProjection, topProjection - midFrontProjection).normalized;
        sideNormal = Vector3.Cross(rightProjection - midSideProjection, leftProjection - midSideProjection).normalized;

        //calcula angulos do eixo frontal
        topAngle = RotationMath.ComputeRotationAngle(topProjection, midFrontProjection, frontNormal);
        if (topAngle < 0) topAngle += 360;
        botAngle = RotationMath.ComputeRotationAngle(botProjection, midFrontProjection, frontNormal);
        if (botAngle > 0) botAngle -= 360;
        frontAnguloLimite = (topAngle + botAngle) * 0.5f + 180f;

        //calcula angulos do eixo lateral
        leftAngle = RotationMath.ComputeRotationAngle(leftProjection, midSideProjection, sideNormal);
        if (leftAngle < 0) leftAngle += 360;
        rightAngle = RotationMath.ComputeRotationAngle(rightProjection, midSideProjection, sideNormal);
        if (rightAngle > 0) rightAngle -= 360;
        sideAnguloLimite = (leftAngle + rightAngle) * 0.5f + 180f;
    }

    public float GetFront(Vector2 currentRot)
    {
        return GetFront(RotationMath.rot2pos(currentRot));
    }
    float GetFront(Vector3 currentPos)
    {
        currentFrontProjection = RotationMath.closestInPlane(currentPos, frontNormal).normalized * 0.5f;
        float currentFrontAngle = RotationMath.ComputeRotationAngle(currentFrontProjection, midFrontProjection, frontNormal);
        if (frontAnguloLimite > 0 && currentFrontAngle < 0) currentFrontAngle += 360f;
        if (frontAnguloLimite < 0 && currentFrontAngle > 0) currentFrontAngle -= 360f;

        return 0.5f + (currentFrontAngle > frontAnguloLimite
                ? (360f - currentFrontAngle) / botAngle
                : currentFrontAngle / topAngle
                ) * 0.5f;
    }
    public float GetSide(Vector2 currentRot)
    {
        return GetSide(RotationMath.rot2pos(currentRot));
    }
    float GetSide(Vector3 currentPos)
    {
        currentSideProjection = RotationMath.closestInPlane(currentPos, sideNormal).normalized * 0.5f;
        float currentSideAngle = RotationMath.ComputeRotationAngle(currentSideProjection, midSideProjection, sideNormal);
        if (sideAnguloLimite > 0 && currentSideAngle < 0) currentSideAngle += 360f;
        if (sideAnguloLimite < 0 && currentSideAngle > 0) currentSideAngle -= 360f;

        return 0.5f + (currentSideAngle > sideAnguloLimite
                ? (360f - currentSideAngle) / leftAngle
                : currentSideAngle / rightAngle
                ) * 0.5f;
    }

    public Analise GetEvaluation(Vector2 rot)
    {
        Vector3 currentPos = RotationMath.rot2pos(rot);

        Analise a;
        a.progressoFrontal = GetFront(currentPos);
        a.progressoLateral = GetSide(currentPos);

        // cria um numero entre 0 e 1 para a proximidade de cada eixo: 0 está muito mais perto do eixo frontal, 1 muito mais perto de eixo lateral
        if (Math.Abs(a.progressoFrontal - 0.5f) < 0.1f && Math.Abs(a.progressoLateral - 0.5f) < 0.1f)
            a.ProbabilidadeEixoLateral = 0.5f;
        else
        {
            // Calcula proximidade com cada um dos planos.
            // A menor indica o movimento mais provavel de estar sendo feito
            // (mais inconsistente quando no meio)
            float frontDistance = Vector3.Distance(currentFrontProjection, currentPos);
            float sideDistance = Vector3.Distance(currentSideProjection, currentPos);
            float sum = frontDistance + sideDistance;

            a.ProbabilidadeEixoLateral = (1f - (sideDistance - frontDistance) / sum) * 0.5f;
        }

        return a;
    }

    public Direcao ObterDirecao(Analise a, bool prioridade = false, bool front = false)
    {
        if (prioridade)
        {
            if (front)
            {
                if (a.progressoFrontal > thresholds.top)
                    return Direcao.top;
                if (a.progressoFrontal < thresholds.bot)
                    return Direcao.bot;
            }
            else
            {
                if (a.progressoLateral > thresholds.right)
                    return Direcao.right;
                if (a.progressoLateral < thresholds.left)
                    return Direcao.left;
            }
        }
        if (a.ProbabilidadeEixoLateral < 0.5f)
        {
            if (a.progressoFrontal > thresholds.top)
                return Direcao.top;
            if (a.progressoFrontal < thresholds.bot)
                return Direcao.bot;
        }
        if (a.ProbabilidadeEixoLateral > 0.5f)
        {
            if (a.progressoLateral > thresholds.right)
                return Direcao.right;
            if (a.progressoLateral < thresholds.left)
                return Direcao.left;
        }
        return Direcao.mid;

    }

    public void Dificultar(Direcao dir, float valorAtingido)
    {
        switch (dir)
        {
            case Direcao.top:
                thresholds.top = Mathf.Max(0.5f + THRESHOLD_SCALING, Math.Max(thresholds.top, Math.Min(thresholds.top + MAX_THRESHOLD_SCALING, (thresholds.top + valorAtingido) * 0.5f)));
                break;
            case Direcao.bot:
                thresholds.bot = Mathf.Min(0.5f - THRESHOLD_SCALING,Math.Min(thresholds.bot, Math.Max(thresholds.bot - MAX_THRESHOLD_SCALING, (thresholds.bot + valorAtingido) * 0.5f)));
                break;
            case Direcao.left:
                thresholds.left = Mathf.Min(0.5f - THRESHOLD_SCALING,Math.Min(thresholds.left, Math.Max(thresholds.left - MAX_THRESHOLD_SCALING, (thresholds.left + valorAtingido) * 0.5f)));
                break;
            case Direcao.right:
                thresholds.right = Mathf.Max(0.5f + THRESHOLD_SCALING,Math.Max(thresholds.right, Math.Min(thresholds.right + MAX_THRESHOLD_SCALING, (thresholds.right + valorAtingido) * 0.5f)));
                break;
        }
    }

    public void Dificultar(Direcao dir)
    {
        switch (dir)
        {
            case Direcao.top:
                thresholds.top += THRESHOLD_SCALING;
                break;
            case Direcao.bot:
                thresholds.bot -= THRESHOLD_SCALING;
                break;
            case Direcao.left:
                thresholds.left -= THRESHOLD_SCALING;
                break;
            case Direcao.right:
                thresholds.right += THRESHOLD_SCALING;
                break;
        }
    }
    public void Facilitar(Direcao dir)
    {
        switch (dir)
        {
            case Direcao.top:
                thresholds.top -= THRESHOLD_SCALING;
                break;
            case Direcao.bot:
                thresholds.bot += THRESHOLD_SCALING;
                break;
            case Direcao.left:
                thresholds.left += THRESHOLD_SCALING;
                break;
            case Direcao.right:
                thresholds.right -= THRESHOLD_SCALING;
                break;
        }
    }

    public float ObterAngulo(Direcao dir, float progresso)
    {
        return dir switch
        {
            Direcao.top => topAngle * (progresso - 0.5f) * 2f,
            Direcao.bot => botAngle * (1f - progresso * 2f),
            Direcao.right => rightAngle * (progresso - 0.5f) * 2f,
            Direcao.left => leftAngle * (1f - progresso * 2f),
            _ => 0.5f
        };
    }

#if UNITY_EDITOR
    void DrawGizmosPlane2(Plane p, float planeSize = 0.6f)
    {
        Quaternion rotation = p.normal==Vector3.zero?Quaternion.identity:Quaternion.LookRotation(p.normal);
        Matrix4x4 trs = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
        Gizmos.matrix = trs;
        // Gizmos.DrawCube(Vector3.zero, new Vector3(planeSize, planeSize, 0.0001f));
        Vector3 lastPoint = Vector3.zero;

        for (int i = 0; i <= 360; i += 10)
        {
            float angle = i * Mathf.Deg2Rad;
            Vector3 newPoint = new Vector3(Mathf.Cos(angle) * planeSize, Mathf.Sin(angle) * planeSize, 0);

            if (i > 0)
            {
                Gizmos.DrawLine(lastPoint, newPoint);
                Gizmos.DrawLine(Vector3.zero, newPoint);
            }

            lastPoint = newPoint;
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
    public Thresholds GetThresholds()
    {
        return thresholds;
    }
    public void DrawGizmos(Vector3 pos)
    {
        Vector2 currentRot = GameObject.FindObjectOfType<ControladorSensores>().dados;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(pos, .5f);

        Vector3 topPos = RotationMath.rot2pos(topRot);
        Vector3 BotPos = RotationMath.rot2pos(botRot);
        Vector3 midPos = RotationMath.rot2pos(midRot);
        Vector3 leftPos = RotationMath.rot2pos(leftRot);
        Vector3 rightPos = RotationMath.rot2pos(rightRot);
        Vector3 currentPos = RotationMath.rot2pos(currentRot);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos + topPos, 0.03f);
        Gizmos.DrawWireSphere(pos + BotPos, 0.03f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(pos + midPos, 0.03f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(pos + leftPos, 0.03f);
        Gizmos.DrawWireSphere(pos + rightPos, 0.03f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos + currentPos, 0.03f);

        Vector3 frontNormal = Vector3.Cross((topPos + midPos) * 0.5f, (BotPos + midPos) * 0.5f).normalized;
        Vector3 sideNormal = Vector3.Cross((leftPos + midPos) * 0.5f, (rightPos + midPos) * 0.5f).normalized;

        // desenha os dois planos
        Gizmos.color = Color.green;
        DrawGizmosPlane2(new Plane(frontNormal, 0f));
        DrawGizmosPlane2(new Plane(sideNormal, 0f));

        //projeta pontos no plano frontal
        Vector3 topProjection = RotationMath.closestInPlane(topPos, frontNormal).normalized * 0.5f;
        Vector3 botProjection = RotationMath.closestInPlane(BotPos, frontNormal).normalized * 0.5f;
        Vector3 midFrontProjection = RotationMath.closestInPlane(midPos, frontNormal).normalized * 0.5f;
        Vector3 currentFrontProjection = RotationMath.closestInPlane(currentPos, frontNormal).normalized * 0.5f;

        //projeta pontos no plano lateral
        Vector3 leftProjection = RotationMath.closestInPlane(leftPos, sideNormal).normalized * 0.5f;
        Vector3 rightProjection = RotationMath.closestInPlane(rightPos, sideNormal).normalized * 0.5f;
        Vector3 midSideProjection = RotationMath.closestInPlane(midPos, sideNormal).normalized * 0.5f;
        Vector3 currentSideProjection = RotationMath.closestInPlane(currentPos, sideNormal).normalized * 0.5f;

        // Dependendo dos angulos, as normais podem apontar para um extremo ou outro.
        // Usando esse metodo, obtem normais com direção consistente
        frontNormal = Vector3.Cross(botProjection - midFrontProjection, topProjection - midFrontProjection).normalized;
        sideNormal = Vector3.Cross(rightProjection - midSideProjection, leftProjection - midSideProjection).normalized;

        //calcula angulos do eixo frontal
        float topAngle = RotationMath.ComputeRotationAngle(topProjection, midFrontProjection, frontNormal);
        if (topAngle < 0) topAngle += 360;
        float botAngle = RotationMath.ComputeRotationAngle(botProjection, midFrontProjection, frontNormal);
        if (botAngle > 0) botAngle -= 360;
        float currentFrontAngle = RotationMath.ComputeRotationAngle(currentFrontProjection, midFrontProjection, frontNormal);
        //corrige atual com limite oposto entre max e min do frontal
        float frontAnguloLimite = (topAngle + botAngle) * 0.5f + 180f;
        if (frontAnguloLimite > 0 && currentFrontAngle < 0) currentFrontAngle += 360f;
        if (frontAnguloLimite < 0 && currentFrontAngle > 0) currentFrontAngle -= 360f;

        //calcula angulos do eixo lateral
        float leftAngle = RotationMath.ComputeRotationAngle(leftProjection, midSideProjection, sideNormal);
        if (leftAngle < 0) leftAngle += 360;
        float rightAngle = RotationMath.ComputeRotationAngle(rightProjection, midSideProjection, sideNormal);
        if (rightAngle > 0) rightAngle -= 360;
        float currentSideAngle = RotationMath.ComputeRotationAngle(currentSideProjection, midSideProjection, sideNormal);
        //corrige atual com limite oposto entre max e min do lateral
        float sideAnguloLimite = (leftAngle + rightAngle) * 0.5f + 180f;
        if (sideAnguloLimite > 0 && currentSideAngle < 0) currentSideAngle += 360f;
        if (sideAnguloLimite < 0 && currentSideAngle > 0) currentSideAngle -= 360f;

        float frontProgress = 0.5f + (currentFrontAngle > frontAnguloLimite
            ? (360f - currentFrontAngle) / botAngle
            : currentFrontAngle / topAngle
            ) * 0.5f;

        float sideProgress = 0.5f + (currentSideAngle > sideAnguloLimite
            ? (360f - currentSideAngle) / rightAngle
            : currentSideAngle / leftAngle
            ) * 0.5f;

    }
#endif
}
