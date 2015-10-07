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
using TgcViewer.Utils.Collision.ElipsoidCollision;

namespace AlumnoEjemplos.MiGrupo
{
    /// <summary>
    /// Ejemplo del alumno
    /// </summary>
    public class EjemploAlumno : TgcExample
    {
        //VARIABLES GLOBALES
            
             //skybox
             TgcSkyBox skyBox;
            
             //Moto
             TgcMesh motorcycle;

             //collisions
             TgcElipsoid characterElipsoid;
             List<Collider> objetosColisionables = new List<Collider>();
             ElipsoidCollisionManager collisionManager;
            
             //Ciudad
             TgcScene scene;

             //Debug
             TgcArrow collisionNormalArrow;
             TgcArrow directionArrow;
             TgcBox collisionPoint;

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
        /// 
        public void inicializarSkybox(String texturesPath)
        {
            skyBox = new TgcSkyBox();
            skyBox.Center = new Vector3(0, -1000, 0);
            skyBox.Size = new Vector3(10000, 10000, 10000);
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "phobos_up.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "phobos_dn.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "phobos_lf.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "phobos_rt.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "phobos_bk.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "phobos_ft.jpg");
            skyBox.updateValues();
        }
        public override void init()
        {
            //GuiController.Instance: acceso principal a todas las herramientas del Framework

            //Herramientas
            Microsoft.DirectX.Direct3D.Device d3dDevice = GuiController.Instance.D3dDevice;
            string texturesPath = GuiController.Instance.ExamplesMediaDir + "Texturas\\Quake\\SkyBox1\\";
            TgcSceneLoader loader = new TgcSceneLoader();
            collisionManager = new ElipsoidCollisionManager();
            collisionManager.GravityEnabled = true;
           
            //skybox
            inicializarSkybox(texturesPath);
            
            //carga la ciudad
            scene = loader.loadSceneFromFile(GuiController.Instance.AlumnoEjemplosMediaDir + "ciudad\\Ciudad2-TgcScene.xml");

            //cargo la moto
            motorcycle = loader.loadSceneFromFile(GuiController.Instance.AlumnoEjemplosMediaDir + "moto\\Moto2-TgcScene.xml").Meshes[0];
            motorcycle.move(0, 1000, 500);
           
            //camara
            GuiController.Instance.ThirdPersonCamera.Enable = true;
            GuiController.Instance.ThirdPersonCamera.setCamera(motorcycle.Position, 10, 250);
            GuiController.Instance.ThirdPersonCamera.rotateY(-1.57f);

            //creo la bounding elipsoid
            motorcycle.AutoUpdateBoundingBox = false;
            characterElipsoid = new TgcElipsoid(motorcycle.BoundingBox.calculateBoxCenter() + new Vector3(0, 0, 0), new Vector3(23 ,23,23));
            
            //cargo los colliders
            objetosColisionables.Clear();
            foreach (TgcMesh mesh in scene.Meshes)
            {
                //Los objetos del layer "TriangleCollision" son colisiones a nivel de triangulo
                if (mesh.Layer == "TriangleCollision")
                {
                    objetosColisionables.Add(TriangleMeshCollider.fromMesh(mesh));
                }
                //El resto de los objetos son colisiones de BoundingBox. Las colisiones a nivel de triangulo son muy costosas asi que deben utilizarse solo
                //donde es extremadamente necesario (por ejemplo en el piso). El resto se simplifica con un BoundingBox
                else
                {
                    objetosColisionables.Add(BoundingBoxCollider.fromBoundingBox(mesh.BoundingBox));
                }
            }

            //Modifier para ver BoundingBox
            GuiController.Instance.Modifiers.addBoolean("Collisions", "Collisions", true);
            GuiController.Instance.Modifiers.addBoolean("showBoundingBox", "Bouding Box", true);

            //Modifiers para desplazamiento del personaje
            GuiController.Instance.Modifiers.addFloat("VelocidadCaminar", 0, 20, 2);
            GuiController.Instance.Modifiers.addFloat("VelocidadRotacion", 1f, 360f, 150f);
            GuiController.Instance.Modifiers.addBoolean("HabilitarGravedad", "Habilitar Gravedad", true);
            GuiController.Instance.Modifiers.addVertex3f("Gravedad", new Vector3(-50, -50, -50), new Vector3(50, 50, 50), new Vector3(0, -1, 0));
            GuiController.Instance.Modifiers.addFloat("SlideFactor", 0f, 25f, 1f);
            GuiController.Instance.Modifiers.addFloat("Pendiente", 0f, 10f, 0.72f);
            GuiController.Instance.Modifiers.addFloat("VelocidadSalto", 0f, 50f, 10f);
            GuiController.Instance.Modifiers.addFloat("TiempoSalto", 0f, 2f, 0.5f);


            GuiController.Instance.UserVars.addVar("Movement");
            GuiController.Instance.UserVars.addVar("Angulo");


            //DEBUG
            //Crear linea para mostrar la direccion del movimiento del personaje
            directionArrow = new TgcArrow();
            directionArrow.BodyColor = Color.Red;
            directionArrow.HeadColor = Color.Green;
            directionArrow.Thickness = 0.4f;
            directionArrow.HeadSize = new Vector2(5, 10);

            //Linea para normal de colision
            collisionNormalArrow = new TgcArrow();
            collisionNormalArrow.BodyColor = Color.Blue;
            collisionNormalArrow.HeadColor = Color.Yellow;
            collisionNormalArrow.Thickness = 0.4f;
            collisionNormalArrow.HeadSize = new Vector2(2, 5);

            //Caja para marcar punto de colision
            collisionPoint = TgcBox.fromSize(new Vector3(4, 4, 4), Color.Red);
            //TERMINA DEBUG

        }

        public float anguloEntreVectores(Vector3 v1,Vector3 v2)
        {
            var producto = Vector3.Dot(v1, v2);
            var prodModulos = v1.Length() * v2.Length();
            return (FastMath.Acos(producto / prodModulos));
        }

        /// <summary>
        /// Método que se llama cada vez que hay que refrescar la pantalla.
        /// Escribir aquí todo el código referido al renderizado.
        /// Borrar todo lo que no haga falta
        /// </summary>
        /// <param name="elapsedTime">Tiempo en segundos transcurridos desde el último frame</param>
        public override void render(float elapsedTime)
        {
            Microsoft.DirectX.Direct3D.Device d3dDevice = GuiController.Instance.D3dDevice;
            TgcD3dInput d3dInput = GuiController.Instance.D3dInput;

            var original_pos = motorcycle.Position;

            //Obtener boolean para saber si hay que mostrar Bounding Box
            bool showBB = (bool)GuiController.Instance.Modifiers.getValue("showBoundingBox");


            //obtener velocidades de Modifiers
            float velocidadCaminar = (float)GuiController.Instance.Modifiers.getValue("VelocidadCaminar");
            float velocidadRotacion = (float)GuiController.Instance.Modifiers.getValue("VelocidadRotacion");
            float velocidadSalto = (float)GuiController.Instance.Modifiers.getValue("VelocidadSalto");
            float tiempoSalto = (float)GuiController.Instance.Modifiers.getValue("TiempoSalto");


            //Calcular proxima posicion de personaje segun Input
            float moveForward = 0f;
            float rotate = 0;
            bool moving = false;
            bool rotating = false;

            //Adelante
            if (d3dInput.keyDown(Key.W))
            {
                moveForward = -velocidadCaminar;
                moving = true;
            }

            //Atras
            if (d3dInput.keyDown(Key.S))
            {
                moveForward = velocidadCaminar;
                moving = true;
            }

            //Derecha
            if (d3dInput.keyDown(Key.D))
            {
                rotate = -velocidadRotacion;
                rotating = true;
            }

            //Izquierda
            if (d3dInput.keyDown(Key.A))
            {
                rotate = velocidadRotacion;
                rotating = true;
            }

            //ROTACION
            if (rotating)
            {
                //Rotar personaje y la camara, hay que multiplicarlo por el tiempo transcurrido para no atarse a la velocidad el hardware
                float rotAngle = Geometry.DegreeToRadian(rotate * elapsedTime);
                motorcycle.rotateX(rotAngle);
            }
            
            //Actualizar valores de gravedad
            collisionManager.GravityEnabled = (bool)GuiController.Instance.Modifiers["HabilitarGravedad"];
            collisionManager.GravityForce = (Vector3)GuiController.Instance.Modifiers["Gravedad"] /** elapsedTime*/;
            collisionManager.SlideFactor = (float)GuiController.Instance.Modifiers["SlideFactor"];
            collisionManager.OnGroundMinDotValue = (float)GuiController.Instance.Modifiers["Pendiente"];

            //CALCULO VECTOR MOVIMIENTO
            Vector3 movementVector = Vector3.Empty;
            if (moving)
            {
                //Aplicar movimiento, desplazarse en base a la rotacion actual del personaje
                movementVector = new Vector3(
                    FastMath.Sin(motorcycle.Rotation.Y) * moveForward,
                    0,
                    FastMath.Cos(motorcycle.Rotation.Y) * moveForward
                    );
            }
            Vector3 realMovement = movementVector;
            //MOVIMIENTO EN SI guarda en realMovement el movimiento que no colisiona
            if ((bool)GuiController.Instance.Modifiers["Collisions"])
            {
                realMovement = collisionManager.moveCharacter(characterElipsoid, movementVector, objetosColisionables);
                motorcycle.move(realMovement);

                //Cargar desplazamiento realizar en UserVar
                GuiController.Instance.UserVars.setValue("Movement", TgcParserUtils.printVector3(realMovement));
            }
            else
            {
                motorcycle.move(movementVector);
            }
            if (moving && collisionManager.Result.collisionFound)
            {
                motorcycle.Rotation = new Vector3(anguloEntreVectores(collisionManager.Result.collisionNormal, new Vector3(0, 1, 0)),0,0);
            }

            GuiController.Instance.UserVars.setValue("Angulo", TgcParserUtils.printFloat(anguloEntreVectores(collisionManager.Result.collisionNormal, new Vector3(0, 1, 0))));



            //actualizo la camara
            GuiController.Instance.ThirdPersonCamera.Target = motorcycle.Position;

            //DEBUG
            //Actualizar valores de la linea de movimiento
            directionArrow.PStart = characterElipsoid.Center;
            directionArrow.PEnd = characterElipsoid.Center + Vector3.Multiply(movementVector, 50);
            directionArrow.updateValues();

            //Actualizar valores de normal de colision
            if (collisionManager.Result.collisionFound)
            {
                collisionNormalArrow.PStart = collisionManager.Result.collisionPoint;
                collisionNormalArrow.PEnd = collisionManager.Result.collisionPoint + Vector3.Multiply(collisionManager.Result.collisionNormal, 80); ;
                collisionNormalArrow.updateValues();
                collisionNormalArrow.render();

                collisionPoint.Position = collisionManager.Result.collisionPoint;
                collisionPoint.render();
            }

            directionArrow.render();
            //TERMINA DEBUG

            

            //Renders
            motorcycle.render();
            scene.renderAll();
            skyBox.render();
            if (showBB)
            {
                characterElipsoid.render();
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
            skyBox.dispose();
            characterElipsoid.dispose();
            collisionNormalArrow.dispose();
            directionArrow.dispose();
        }

    }
}
