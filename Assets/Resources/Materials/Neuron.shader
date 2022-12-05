Shader "Unlit/Neuron"
{
    Properties
    {
        _NeuronColor ("Neuron Color", Color) = (0.3, 0.3, 1, 1)
        _SignalColor ("Siganl Color", Color) = (1, 0.3, 0.3, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : TEXCOORD;
            };

            fixed4 _NeuronColor;
            fixed4 _SignalColor;
            
            int cometLen;

            StructuredBuffer<int> vert2Neurons;
            StructuredBuffer<int> neuron2SignalLvls;

            v2f vert (appdata v, uint vIdx : SV_VERTEXID)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                int nIdx = vert2Neurons[vIdx];
                fixed signalK = cometLen == 0 ? 0 :
                    (float)neuron2SignalLvls[nIdx] / (float)cometLen;
                o.color = (1 - signalK) * _NeuronColor + signalK * _SignalColor;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = i.color;
                return col;
            }
            ENDCG
        }
    }
}
