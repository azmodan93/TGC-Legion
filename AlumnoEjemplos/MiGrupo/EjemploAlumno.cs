using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Example;
using TgcViewer;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using Microsoft.DirectX;
using TgcViewer.Utils.Modifiers;
using TgcViewer.Utils.TgcSceneLoader;
using TgcViewer.Utils.Input;
using Microsoft.DirectX.DirectInput;
using TgcViewer.Utils.Terrain;
using TgcViewer.Utils.TgcGeometry;

namespace AlumnoEjemplos.MiGrupo
{
    /// <summary>
    /// Ejemplo del alumno
    /// </summary>
    public class EjemploAlumno : TgcExample
    {
        TgcSkyBox skyBox;
        TgcMesh motorcycle;
        float vel = 100f;
        float vel_an = 2f;
        TgcScene scene;
        /// <summary>
        /// Categoría a la que pertenece el ejemplo.
        /// Influye en donde se va a haber en el árbol de la derecha de la pantalla.
        /// </summary>
        public override string getCategory()
        {
            return "AlumnoEjemplos";
        }

        /// <summary>
        /// Completar nombre del grupo en formato Grupo NN
        /// </summary>
        public override string getName()
        {
            return "Legion Group";
        }

        /// <summary>
        /// Completar con la descripción del TP
        /// </summary>
        public override string getDescription()
        {
            return "MiIdea - Descripcion de la idea";
        }

        /// <summary>
        /// Método que se llama una sola vez,  al principio cuando se ejecuta el ejemplo.
        /// Escribir aquí todo el código de inicialización: cargar modelos, texturas, modifiers, uservars, etc.
        /// Borrar todo lo que no haga falta
        /// </summary>
        public override void init()
        {
            //GuiController.Instance: acceso principal a todas las herramientas del Framework

            //Device de DirectX para crear primitivas
            Microsoft.DirectX.Direct3D.Device d3dDevice = GuiController.Instance.D3dDevice;
            string texturesPath = GuiController.Instance.ExamplesMediaDir + "Texturas\\Quake\\SkyBox1\\";

            //skybox
            skyBox = new TgcSkyBox();
            skyBox.Center = new Vector3(0, -1000, 0);
            skyBox.Size = new Vector3(10000, 10000, 10000);
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "phobos_up.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "phobos_dn.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "phobos_lf.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "phobos_rt.jpg");

            //Hay veces es necesario invertir las texturas Front y Back si se pasa de un sistema RightHanded a uno LeftHanded
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "phobos_bk.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "phobos_ft.jpg");
            skyBox.updateValues();
            TgcSceneLoader loader = new TgcSceneLoader();
            
            //carga la ciudad
            scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "MeshCreator\\Scenes\\Ciudad\\Ciudad-TgcScene.xml");
            //De toda la escena solo nos interesa guardarnos el primer modelo (el único que hay en este caso).
            motorcycle = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "MeshCreator\\Meshes\\Vehiculos\\Moto\\Moto-TgcScene.xml").Meshes[0];
            //Hacemos que la cámara esté centrada sobre este modelo.
            motorcycle.move(0, 5, 0);
            
            //camara
            GuiController.Instance.ThirdPersonCamera.Enable = true;
            GuiController.Instance.ThirdPersonCamera.setCamera(motorcycle.Position, 10, 250);
            GuiController.Instance.ThirdPersonCamera.rotateY(-1.57f);

        }


        /// <summary>
        /// Método que se llama cada vez que hay que refrescar la pantalla.
        /// Escribir aquí todo el código referido al renderizado.
        /// Borrar todo lo que no haga falta
        /// </summary>
        /// <param name="elapsedTime">Tiempo en segundos transcurridos desde el último frame</param>
        public override void render(float elapsedTime)
        {
            //Device de DirectX para renderizar
            Microsoft.DirectX.Direct3D.Device d3dDevice = GuiController.Instance.D3dDevice;
             TgcD3dInput input = GuiController.Instance.D3dInput;

             Vector3 original_pos = motorcycle.Position;

             if (input.keyDown(Key.Down))
             {
                 motorcycle.moveOrientedY(vel * elapsedTime/2);
             }

             if (input.keyDown(Key.Up))
             {
                 motorcycle.moveOrientedY(-vel * elapsedTime);
             }

             if (input.keyDown(Key.Right))
             {
                 motorcycle.rotateX(-vel_an * elapsedTime);
             }

             if (input.keyDown(Key.Left))
             {
                 motorcycle.rotateX(vel_an * elapsedTime);
             }


             bool collisionFound = false;
             foreach (TgcMesh mesh in scene.Meshes)
             {
                 //Los dos BoundingBox que vamos a testear
                 TgcBoundingBox mainMeshBoundingBox = motorcycle.BoundingBox;
                 TgcBoundingBox sceneMeshBoundingBox = mesh.BoundingBox;

                 //Ejecutar algoritmo de detección de colisiones
                 TgcCollisionUtils.BoxBoxResult collisionResult = TgcCollisionUtils.classifyBoxBox(mainMeshBoundingBox, sceneMeshBoundingBox);

                 //Hubo colisión con un objeto. Guardar resultado y abortar loop.
                 if (collisionResult != TgcCollisionUtils.BoxBoxResult.Afuera)
                 {
                     collisionFound = true;
                     break;
                 }
             }

             //Si hubo alguna colisión, entonces restaurar la posición original del mesh
             if (collisionFound)
             {
                 motorcycle.Position = original_pos;
             }





            GuiController.Instance.ThirdPersonCamera.Target = motorcycle.Position;
           
          
            motorcycle.render();
            scene.renderAll();
            skyBox.render();
            //Obtener valor de UserVar (hay que castear)
            //int valor = (int)GuiController.Instance.UserVars.getValue("variablePrueba");

            /*
            //Obtener valores de Modifiers
            float valorFloat = (float)GuiController.Instance.Modifiers["valorFloat"];
            string opcionElegida = (string)GuiController.Instance.Modifiers["valorIntervalo"];
            Vector3 valorVertice = (Vector3)GuiController.Instance.Modifiers["valorVertice"];
            */

            ///////////////INPUT//////////////////
            //conviene deshabilitar ambas camaras para que no haya interferencia

            //Capturar Input teclado 
            if (GuiController.Instance.D3dInput.keyPressed(Microsoft.DirectX.DirectInput.Key.F))
            {
                //Tecla F apretada
            }

            //Capturar Input Mouse
            if (GuiController.Instance.D3dInput.buttonPressed(TgcViewer.Utils.Input.TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                //Boton izq apretado
            }

        }

        /// <summary>
        /// Método que se llama cuando termina la ejecución del ejemplo.
        /// Hacer dispose() de todos los objetos creados.
        /// </summary>
        public override void close()
        {
            motorcycle.dispose();
            scene.disposeAll();
        }

    }
}
