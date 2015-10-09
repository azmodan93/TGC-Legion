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
                 //movimiento
                 float tiempoAcelerando = 0f;
                 float tiempoDescelerando = 0f;
                 float velIni = 0f;
                 bool motoRota;
                 float tiempoRota;
             //collisions
             TgcElipsoid characterElipsoid;
             List<Collider> objetosColisionables = new List<Collider>();
             ElipsoidCollisionManager collisionManager;
            bool tocandoPiso = false;
            Vector3 ultimoMov = new Vector3(0,0,0);
           //  bool terminoDeSaltar = true;
            // int framesParaSaltar = 50;
             
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
            collisionManager.GravityEnabled = false;
            tiempoAcelerando = 0f;
            tiempoDescelerando = 0f;
            velIni = 0f;
            tocandoPiso = false;
            ultimoMov = new Vector3(0, 0, 0);
            motoRota = false;
            tiempoRota = 0f;

            //skybox
            inicializarSkybox(texturesPath);
            
            //carga la ciudad
            scene = loader.loadSceneFromFile(GuiController.Instance.AlumnoEjemplosMediaDir + "ciudad\\Ciudad2-TgcScene.xml");

            //cargo la moto
            motorcycle = loader.loadSceneFromFile(GuiController.Instance.AlumnoEjemplosMediaDir + "moto\\Moto2-TgcScene.xml").Meshes[0];
            motorcycle.move(0, 100
                , 700);
           
            //camara
            GuiController.Instance.ThirdPersonCamera.Enable = true;
            GuiController.Instance.ThirdPersonCamera.setCamera(motorcycle.Position, 10, 175);
            GuiController.Instance.ThirdPersonCamera.rotateY(-1.57f);

            //creo la bounding elipsoid

            motorcycle.Scale = new Vector3(0.5f, 0.5f, 0.5f);

            motorcycle.AutoUpdateBoundingBox = false;
            characterElipsoid = new TgcElipsoid(motorcycle.BoundingBox.calculateBoxCenter() + new Vector3(0, 0, 0), new Vector3(23, 23, 23) * 0.5f);
           
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
            GuiController.Instance.Modifiers.addBoolean("HabilitarGravedad", "Habilitar Gravedad", true);

            //Modifiers para desplazamiento del personaje
            GuiController.Instance.Modifiers.addFloat("VelocidadMax", 0f, 100f, 20f);
            GuiController.Instance.Modifiers.addFloat("Aceleracion", 0f, 10f, 0.5f);

            GuiController.Instance.Modifiers.addFloat("Rozamiento", 0.1f, 2f, 0.1f);
            GuiController.Instance.Modifiers.addFloat("VelocidadRotacion", 1f,500f, 300f);
            GuiController.Instance.Modifiers.addVertex3f("Gravedad", new Vector3(-50, -50, -50), new Vector3(50, 50, 50), new Vector3(0, -0.3f, 0));

            GuiController.Instance.Modifiers.addFloat("SlideFactor", 0f, 10f, 1f);
            GuiController.Instance.Modifiers.addFloat("Pendiente", 0f, 1f, 0.72f);
           
            GuiController.Instance.UserVars.addVar("Movement");
            GuiController.Instance.UserVars.addVar("AnguloZ");
            GuiController.Instance.UserVars.addVar("AnguloY");

            GuiController.Instance.UserVars.addVar("moveForward");
            GuiController.Instance.UserVars.addVar("velIni");
            GuiController.Instance.UserVars.addVar("aceleracion");
            GuiController.Instance.UserVars.addVar("tiempoAcel");
            GuiController.Instance.UserVars.addVar("tiempoDesce");



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
            var original_rot = motorcycle.Rotation;

           
            bool showBB = (bool)GuiController.Instance.Modifiers.getValue("showBoundingBox");
            float aceleracion = 0f;
            float moveForward=0f;
            float rotate = 0;
            bool moving = false;
            bool rotating = false;
            float aceleracionVar = (float)GuiController.Instance.Modifiers.getValue("Aceleracion");
            float velMax = (float)GuiController.Instance.Modifiers.getValue("VelocidadMax");
            float rozamiento = (float)GuiController.Instance.Modifiers.getValue("Rozamiento");
            float velocidadRotacion = (float)GuiController.Instance.Modifiers.getValue("VelocidadRotacion");


            if (motoRota)
            {
                tiempoRota += elapsedTime;
                if (tiempoRota > 5f)
                {
                    init();
                }
            }
            else
            {

                if (d3dInput.keyUp(Key.W))
                {
                    tiempoAcelerando = 0f;
                }

                if (d3dInput.keyUp(Key.S))
                {
                    tiempoDescelerando = 0f;
                }



                //Adelante
                if (d3dInput.keyDown(Key.W) && tocandoPiso)
                {
                    aceleracion = aceleracionVar;
                    moveForward = -aceleracion * tiempoAcelerando + velIni;
                    if (moveForward < -velMax)
                    {
                        moveForward = -velMax;
                    }
                    velIni = moveForward;
                    tiempoAcelerando += elapsedTime;
                    tiempoDescelerando = 0f;

                }

                //Atras
                if (d3dInput.keyDown(Key.S) && tocandoPiso)
                {
                    aceleracion = -aceleracionVar;
                    moveForward = -aceleracion * tiempoDescelerando + velIni;
                    if (moveForward > velMax / 2)
                    {
                        moveForward = velMax / 2;
                    }
                    velIni = moveForward;
                    tiempoDescelerando += elapsedTime;
                    tiempoAcelerando = 0f;
                }

                if ((!d3dInput.keyDown(Key.S) && !d3dInput.keyDown(Key.W)) || !tocandoPiso)
                {
                    aceleracion = 0f;
                    moveForward = velIni;

                }

                if (moveForward != 0 && tocandoPiso)
                {
                    if (moveForward < 0)
                    {
                        moveForward += rozamiento;
                    }
                    if (moveForward > 0)
                    {
                        moveForward -= rozamiento;
                    }
                    velIni = moveForward;
                }
                if (moveForward >= rozamiento || moveForward <= -rozamiento)
                {
                    moving = true;
                }
                else
                {
                    moveForward = 0;
                    velIni = moveForward;
                }



                GuiController.Instance.UserVars.setValue("moveForward", TgcParserUtils.printFloat(moveForward));
                GuiController.Instance.UserVars.setValue("velIni", TgcParserUtils.printFloat(velIni));
                GuiController.Instance.UserVars.setValue("aceleracion", TgcParserUtils.printFloat(aceleracion));
                GuiController.Instance.UserVars.setValue("tiempoAcel", TgcParserUtils.printFloat(tiempoAcelerando));
                GuiController.Instance.UserVars.setValue("tiempoDesce", TgcParserUtils.printFloat(tiempoDescelerando));



                //Derecha
                if (d3dInput.keyDown(Key.D)&&!tocandoPiso)
                {
                    rotate = -velocidadRotacion;
                    rotating = true;
                }

                //Izquierda
                if (d3dInput.keyDown(Key.A)&&!tocandoPiso)
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
                if (moving && tocandoPiso)
                {
                    //Aplicar movimiento, desplazarse en base a la rotacion actual del personaje
                    movementVector = new Vector3(
                        0,
                        -FastMath.Sin(motorcycle.Rotation.X) * moveForward,
                        FastMath.Cos(motorcycle.Rotation.X) * moveForward
                        );
                }
                if (moving && !tocandoPiso)
                {
                    //Aplicar movimiento, desplazarse en base a la rotacion actual del personaje
                    movementVector = ultimoMov;
                }
                Vector3 realMovement = movementVector;

                //    if (!terminoDeSaltar)
                //  {
                //    if(framesParaSaltar == 0)
                //  {
                //    terminoDeSaltar = true;
                //  framesParaSaltar = 51;
                //}
                // framesParaSaltar--;
                //}
                //MOVIMIENTO EN SI guarda en realMovement el movimiento que no colisiona
                if ((bool)GuiController.Instance.Modifiers["Collisions"])// && terminoDeSaltar)
                {
                    realMovement = collisionManager.moveCharacter(characterElipsoid, movementVector, objetosColisionables);
                    motorcycle.move(realMovement);
                    ultimoMov = realMovement;

                    //Cargar desplazamiento realizar en UserVar
                    GuiController.Instance.UserVars.setValue("Movement", TgcParserUtils.printVector3(realMovement));
                }
                else
                {

                    //  if (!terminoDeSaltar)
                    //  {
                    //       movementVector += (Vector3)GuiController.Instance.Modifiers.getValue("Gravedad");
                    //   }
                    motorcycle.move(movementVector);
                    ultimoMov = movementVector;

                }


                if (moving && collisionManager.Result.collisionFound)
                {
                    var asd = anguloEntreVectores(collisionManager.Result.collisionNormal, new Vector3(0, 1, 0));
                    if (anguloEntreVectores(collisionManager.Result.collisionNormal, new Vector3(0, 0, 1)) > 1.5708f)
                    {
                        asd = -asd;
                    }

                    if (anguloEntreVectores(new Vector3(asd, 0, 0), motorcycle.Rotation) > 1.57f)
                    {
                        motoRota = true;
                    }
                    else
                    {
                        motorcycle.Rotation = new Vector3(asd, 0, 0);
                    }
                }


                GuiController.Instance.UserVars.setValue("AnguloY", TgcParserUtils.printFloat(anguloEntreVectores(collisionManager.Result.collisionNormal, new Vector3(0, 1, 0))));
                GuiController.Instance.UserVars.setValue("AnguloZ", TgcParserUtils.printFloat(anguloEntreVectores(collisionManager.Result.collisionNormal, new Vector3(0, 0, 1))));

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

                    collisionPoint.Position = collisionManager.Result.collisionPoint;

                    tocandoPiso = true;

                }
                else
                {
                    tocandoPiso = false;
                    tiempoAcelerando = 0f;
                    tiempoDescelerando = 0f;
                }
            }
            collisionNormalArrow.render();

            collisionPoint.render();

            directionArrow.render();
            //TERMINA DEBUG

       //     if (anguloEntreVectores(original_rot, motorcycle.Rotation) > 0.45f || anguloEntreVectores(original_rot, motorcycle.Rotation) < -0.45f)
       //     {
       //         tocandoPiso = false;
       //         motorcycle.Rotation = original_rot;
       //         motorcycle.Position = original_pos;
       //         terminoDeSaltar = false;
       //     }
            

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
