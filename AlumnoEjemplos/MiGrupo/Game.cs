using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using TgcViewer;
using TgcViewer.Utils.Collision.ElipsoidCollision;
using TgcViewer.Utils.Input;
using TgcViewer.Utils.Terrain;
using TgcViewer.Utils.TgcGeometry;
using TgcViewer.Utils.TgcSceneLoader;

namespace AlumnoEjemplos.MiGrupo
{
    public class Game
    {

        //VARIABLES GLOBALES

        //Herramientas Framework
        Microsoft.DirectX.Direct3D.Device d3dDevice;

        //skybox
        TgcSkyBox skyBox;

        //Moto
        TgcMesh motorcycle;

        //movimiento
        float tiempoAcelerando = 0f;
        float tiempoDescelerando = 0f;
        float velIni = 0f;

        //collisions
        TgcElipsoid characterElipsoid;
        List<Collider> objetosColisionables = new List<Collider>();
        ElipsoidCollisionManager collisionManager;
        bool tocandoPiso = false;
        Vector3 ultimaNormal = new Vector3(0, 0, 0);
        Vector3 ultimoMov = new Vector3(0, 0, 0);
        //  bool terminoDeSaltar = true;
        // int framesParaSaltar = 50;

        //Ciudad
        TgcScene scene;

        //Debug
        TgcArrow collisionNormalArrow;
        TgcArrow directionArrow;
        TgcBox collisionPoint;

        public Game()
        {
            d3dDevice = GuiController.Instance.D3dDevice;
            string texturesPath = GuiController.Instance.AlumnoEjemplosMediaDir + "skybox\\";
            TgcSceneLoader loader = new TgcSceneLoader();
            collisionManager = new ElipsoidCollisionManager();
            collisionManager.GravityEnabled = false;
            tiempoAcelerando = 0f;
            tiempoDescelerando = 0f;
            velIni = 0f;
            tocandoPiso = false;
            ultimoMov = new Vector3(0, 0, 0);

            //skybox
            inicializarSkybox(texturesPath);

            //carga la ciudad
            scene = loader.loadSceneFromFile(GuiController.Instance.AlumnoEjemplosMediaDir + "pistaDesierto\\pistaDesierto-TgcScene.xml");

            //cargo la moto
            motorcycle = loader.loadSceneFromFile(GuiController.Instance.AlumnoEjemplosMediaDir + "moto\\Moto2-TgcScene.xml").Meshes[0];
            motorcycle.move(0, 100, 0);

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
            GuiController.Instance.Modifiers.addFloat("Aceleracion", 0f, 10f, 4f);

            GuiController.Instance.Modifiers.addFloat("Rozamiento", 0.1f, 2f, 0.1f);
            GuiController.Instance.Modifiers.addFloat("VelocidadRotacion", 1f, 500f, 300f);
            GuiController.Instance.Modifiers.addVertex3f("Gravedad", new Vector3(-50, -500, -50), new Vector3(50, 500, 50), new Vector3(0, -20f, 0));

            GuiController.Instance.Modifiers.addFloat("SlideFactor", 0f, 10f, 1f);
            GuiController.Instance.Modifiers.addFloat("Pendiente", 0f, 1f, 0.72f);
            GuiController.Instance.UserVars.addVar("elapsedTime");
            GuiController.Instance.UserVars.addVar("GravedadActual");

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

        public void inicializarSkybox(String texturesPath)
        {
            skyBox = new TgcSkyBox();
            skyBox.Center = new Vector3(0, 1000, -6000);
            skyBox.Size = new Vector3(15000, 15000, 25000);
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "top.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "down.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "left.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "right.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "back.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "front.jpg");
            skyBox.updateValues();
        }

        public float anguloEntreVectores(Vector3 v1, Vector3 v2)
        {
            var producto = Vector3.Dot(v1, v2);
            var prodModulos = v1.Length() * v2.Length();
            return (FastMath.Acos(producto / prodModulos));
        }

        public void activar(ref AlumnoEjemplos.MiGrupo.EjemploAlumno.states estado, float elapsedTime)
        {

            TgcD3dInput d3dInput = GuiController.Instance.D3dInput;

            var original_pos = motorcycle.Position;
            var original_rot = motorcycle.Rotation;

            float bigElapsed = elapsedTime * 20 + 0.1f;
            bool showBB = (bool)GuiController.Instance.Modifiers.getValue("showBoundingBox");
            float aceleracion = 0f;
            float moveForward = 0f;
            float rotate = 0;
            bool moving = false;
            bool rotating = false;
            float aceleracionVar = (float)GuiController.Instance.Modifiers.getValue("Aceleracion");
            float velMax = (float)GuiController.Instance.Modifiers.getValue("VelocidadMax");
            float rozamiento = (float)GuiController.Instance.Modifiers.getValue("Rozamiento");
            float velocidadRotacion = (float)GuiController.Instance.Modifiers.getValue("VelocidadRotacion");
            GuiController.Instance.UserVars.setValue("elapsedTime", TgcParserUtils.printFloat(elapsedTime));

            if (d3dInput.keyUp(Key.W))
            {
                tiempoAcelerando = 0f;
            }

            if (d3dInput.keyUp(Key.S))
            {
                tiempoDescelerando = 0f;
            }

            //Adelante
            if (d3dInput.keyDown(Key.W) && collisionManager.isOnTheGround())
            {
                aceleracion = aceleracionVar;
                moveForward = (-aceleracion * tiempoAcelerando + velIni) * bigElapsed;
                if (moveForward < -velMax)
                {
                    moveForward = -velMax;
                }
                velIni = moveForward;
                tiempoAcelerando += elapsedTime;
                tiempoDescelerando = 0f;

            }

            //Atras
            if (d3dInput.keyDown(Key.S) && collisionManager.isOnTheGround())
            {
                aceleracion = -aceleracionVar;
                moveForward = (-aceleracion * tiempoDescelerando + velIni) * bigElapsed;
                if (moveForward > velMax / 2)
                {
                    moveForward = velMax / 2;
                }
                velIni = moveForward;
                tiempoDescelerando += elapsedTime;
                tiempoAcelerando = 0f;
            }

            if ((!d3dInput.keyDown(Key.S) && !d3dInput.keyDown(Key.W)) || !collisionManager.isOnTheGround())
            {
                aceleracion = 0f;
                moveForward = velIni;

            }

            if (moveForward != 0 && tocandoPiso)
            {
                if (moveForward < 0)
                {
                    moveForward += rozamiento * bigElapsed;
                }
                if (moveForward > 0)
                {
                    moveForward -= rozamiento * bigElapsed;
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
            if (d3dInput.keyDown(Key.D) && !collisionManager.isOnTheGround())
            {
                rotate = -velocidadRotacion;
                rotating = true;
            }

            //Izquierda
            if (d3dInput.keyDown(Key.A) && !collisionManager.isOnTheGround())
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
            collisionManager.GravityForce = (Vector3)GuiController.Instance.Modifiers["Gravedad"] * elapsedTime;
            GuiController.Instance.UserVars.setValue("GravedadActual", TgcParserUtils.printFloat(collisionManager.GravityForce.Y));

            collisionManager.SlideFactor = (float)GuiController.Instance.Modifiers["SlideFactor"];
            collisionManager.OnGroundMinDotValue = (float)GuiController.Instance.Modifiers["Pendiente"];

            //CALCULO VECTOR MOVIMIENTO
            Vector3 movementVector = Vector3.Empty;
            if (moving && collisionManager.isOnTheGround())
            {
                //Aplicar movimiento, desplazarse en base a la rotacion actual del personaje
                movementVector = new Vector3(
                    0,
                    -FastMath.Sin(motorcycle.Rotation.X) * moveForward,
                    FastMath.Cos(motorcycle.Rotation.X) * moveForward
                    );
            }
            if (moving && !collisionManager.isOnTheGround())
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

            if (moving && collisionManager.isOnTheGround())
            {
                var asd = anguloEntreVectores(ultimaNormal, new Vector3(0, 1, 0));
                if (anguloEntreVectores(ultimaNormal, new Vector3(0, 0, 1)) > 1.5708f)
                {
                    asd = -asd;
                }

                motorcycle.Rotation = new Vector3(asd, 0, 0);

            }


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

            /*if((d3dInput.keyDown(Key.W) || d3dInput.keyDown(Key.S)) && realMovement == new Vector3(0, 0, 0))
            {
                if(trabada == 150)
                {
                    motorcycle.move(new Vector3(0, 1, 0));
                    characterElipsoid.moveCenter(new Vector3(0, 1, 0));
                    trabada = 0;
                }
                trabada++;
            }*/

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
                // tiempoAcelerando = 0f;
                //  tiempoDescelerando = 0f;
            }
            ultimaNormal = collisionManager.Result.collisionNormal;
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

        public void close()
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
