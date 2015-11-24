using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TgcViewer;
using TgcViewer.Utils;
using TgcViewer.Utils._2D;
using TgcViewer.Utils.Shaders;
using TgcViewer.Utils.Terrain;
using TgcViewer.Utils.TgcGeometry;
using TgcViewer.Utils.TgcSceneLoader;

namespace AlumnoEjemplos.MiGrupo
{
    public class ShadowMap
    {
        string MyMediaDir;
        string MyShaderDir;
        Effect effect;

        TgcScene scene;
        TgcMesh motorcycle;

        // Shadow map
        readonly int SHADOWMAP_SIZE = 1024;
        Texture g_pShadowMap;    // Texture to which the shadow map is rendered
        Surface g_pDSShadow;     // Depth-stencil buffer for rendering to shadow map
        Matrix g_mShadowProj;    // Projection matrix for shadow map
        Vector3 g_LightPos;						// posicion de la luz actual (la que estoy analizando)
        Vector3 g_LightDir;						// direccion de la luz actual
        Matrix g_LightView;						// matriz de view del light
        float near_plane = 2f;
        float far_plane = 15000f;

        float time;


        public ShadowMap(TgcScene pistaMoto, TgcMesh altaMoto)
        {

            Device d3dDevice = GuiController.Instance.D3dDevice;
            MyMediaDir = GuiController.Instance.ExamplesDir + "Shaders\\WorkshopShaders\\Media\\";
            MyShaderDir = GuiController.Instance.ExamplesDir + "Shaders\\WorkshopShaders\\Shaders\\";

            scene = pistaMoto;
            motorcycle = altaMoto;

            //Cargar Shader personalizado
            effect = TgcShaders.loadEffect(GuiController.Instance.AlumnoEjemplosMediaDir + "shaders\\ShadowMap.fx");

            // le asigno el efecto a las mallas 
            foreach (TgcMesh T in scene.Meshes)
            {
                T.Scale = new Vector3(1f, 1f, 1f);
                T.Effect = effect;
            }
            motorcycle.Effect = effect;

            //--------------------------------------------------------------------------------------
            // Creo el shadowmap. 
            // Format.R32F
            // Format.X8R8G8B8
            g_pShadowMap = new Texture(d3dDevice, SHADOWMAP_SIZE, SHADOWMAP_SIZE,
                                        1, Usage.RenderTarget, Format.R32F,
                                        Pool.Default);

            // tengo que crear un stencilbuffer para el shadowmap manualmente
            // para asegurarme que tenga la el mismo tamaño que el shadowmap, y que no tenga 
            // multisample, etc etc.
            g_pDSShadow = d3dDevice.CreateDepthStencilSurface(SHADOWMAP_SIZE,
                                                             SHADOWMAP_SIZE,
                                                             DepthFormat.D24S8,
                                                             MultiSampleType.None,
                                                             0,
                                                             true);
            // por ultimo necesito una matriz de proyeccion para el shadowmap, ya 
            // que voy a dibujar desde el pto de vista de la luz.
            // El angulo tiene que ser mayor a 45 para que la sombra no falle en los extremos del cono de luz
            // de hecho, un valor mayor a 90 todavia es mejor, porque hasta con 90 grados es muy dificil
            // lograr que los objetos del borde generen sombras
            Control panel3d = GuiController.Instance.Panel3d;
            float aspectRatio = (float)panel3d.Width / (float)panel3d.Height;
            g_mShadowProj = Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(90),
                aspectRatio, 100, 10000);
            d3dDevice.Transform.Projection =
                Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(60),
                aspectRatio, near_plane, far_plane);

            GuiController.Instance.RotCamera.targetObject(scene.Meshes[0].BoundingBox);
           

        }

        public void activar(float elapsedTime,TgcText2d textGanaste, TgcText2d textGanaste2,TgcText2d textoContadorTiempo,TgcText2d textoMejorTiempo,TgcMesh piramid, TgcSkyBox skyBox, bool terminoJuego,TgcSprite tablero)
        {
            Device device = GuiController.Instance.D3dDevice;
            Control panel3d = GuiController.Instance.Panel3d;
            float aspectRatio = (float)panel3d.Width / (float)panel3d.Height;
            time += elapsedTime;

            // animo la pos del avion
            if (!terminoJuego)
            {
                g_LightPos = motorcycle.Position + 2f*(new Vector3(100, 60, 0));
                g_LightDir = motorcycle.Position + (new Vector3(0, 0, 0)) - g_LightPos;
                g_LightDir.Normalize();
            }
            // Shadow maps:
            device.EndScene();      // termino el thread anterior

            GuiController.Instance.RotCamera.CameraCenter = new Vector3(0, 0, 0);
            GuiController.Instance.RotCamera.CameraDistance = 100;
            GuiController.Instance.RotCamera.RotationSpeed = 2f;
            GuiController.Instance.CurrentCamera.updateCamera();
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

            //Genero el shadow map
            RenderShadowMap();

            device.BeginScene();
            // dibujo la escena pp dicha
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            RenderScene(false);
            if (terminoJuego)
            {
                textGanaste.render();
                textGanaste2.render();
            }

            GuiController.Instance.Drawer2D.beginDrawSprite();
            //Dibujar sprite (si hubiese mas, deberian ir todos aquí)
            tablero.render();
            //Finalizar el dibujado de Sprites
            GuiController.Instance.Drawer2D.endDrawSprite();

            textoContadorTiempo.render();
            textoMejorTiempo.render();
            piramid.render();
            device.Transform.Projection =
                Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(45.0f),
                TgcD3dDevice.aspectRatio, 1f, 20000f);

            skyBox.render();

        }

        public void RenderShadowMap()
        {
            Device device = GuiController.Instance.D3dDevice;
            // Calculo la matriz de view de la luz
            effect.SetValue("g_vLightPos", new Vector4(g_LightPos.X, g_LightPos.Y, g_LightPos.Z, 1));
            effect.SetValue("g_vLightDir", new Vector4(g_LightDir.X, g_LightDir.Y, g_LightDir.Z, 1));
            g_LightView = Matrix.LookAtLH(g_LightPos, g_LightPos + g_LightDir, new Vector3(0, 0, 1));

            // inicializacion standard: 
            effect.SetValue("g_mProjLight", g_mShadowProj);
            effect.SetValue("g_mViewLightProj", g_LightView * g_mShadowProj);

            // Primero genero el shadow map, para ello dibujo desde el pto de vista de luz
            // a una textura, con el VS y PS que generan un mapa de profundidades. 
            Surface pOldRT = device.GetRenderTarget(0);
            Surface pShadowSurf = g_pShadowMap.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pShadowSurf);
            Surface pOldDS = device.DepthStencilSurface;
            device.DepthStencilSurface = g_pDSShadow;
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            device.BeginScene();

            // Hago el render de la escena pp dicha
            effect.SetValue("g_txShadow", g_pShadowMap);
            RenderScene(true);

            // Termino 

            device.EndScene();
            //TextureLoader.Save("shadowmap.bmp", ImageFileFormat.Bmp, g_pShadowMap);

            // restuaro el render target y el stencil
            device.DepthStencilSurface = pOldDS;
            device.SetRenderTarget(0, pOldRT);
        }

        public void RenderScene(bool shadow)
        {
            foreach (TgcMesh T in scene.Meshes)
            {
                if (shadow)
                {
                    T.Technique = "RenderShadow";
                }
                else
                {
                    T.Technique = "RenderScene";
                }

                T.render();
            }

            // avion
            if (shadow)
            {
                motorcycle.Technique = "RenderShadow";
            }
            else
            {
                motorcycle.Technique = "RenderScene";
            }
            motorcycle.render();
        }

        public Matrix CalcularMatriz(Vector3 Pos, Vector3 Scale, Vector3 Dir)
        {
            Vector3 VUP = new Vector3(0, 1, 0);

            Matrix matWorld = Matrix.Scaling(Scale);
            // determino la orientacion
            Vector3 U = Vector3.Cross(VUP, Dir);
            U.Normalize();
            Vector3 V = Vector3.Cross(Dir, U);
            Matrix Orientacion;
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M21 = V.X;
            Orientacion.M22 = V.Y;
            Orientacion.M23 = V.Z;
            Orientacion.M24 = 0;

            Orientacion.M31 = Dir.X;
            Orientacion.M32 = Dir.Y;
            Orientacion.M33 = Dir.Z;
            Orientacion.M34 = 0;

            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            matWorld = matWorld * Orientacion;

            // traslado
            matWorld = matWorld * Matrix.Translation(Pos);
            return matWorld;
        }

        public void close()
        {
            effect.Dispose();
            scene.disposeAll();
            motorcycle.dispose();
            g_pShadowMap.Dispose();
            g_pDSShadow.Dispose();
        }  

    }
}