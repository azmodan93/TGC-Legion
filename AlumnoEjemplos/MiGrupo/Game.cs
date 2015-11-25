using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using TgcViewer;
using TgcViewer.Utils;
using TgcViewer.Utils._2D;
using TgcViewer.Utils.Collision.ElipsoidCollision;
using TgcViewer.Utils.Input;
using TgcViewer.Utils.Terrain;
using TgcViewer.Utils.TgcGeometry;
using TgcViewer.Utils.TgcSceneLoader;
using TgcViewer.Utils.Ui;

namespace AlumnoEjemplos.MiGrupo
{
    public class Game
    {
        TgcDrawer2D drawer;
        
        //VARIABLES GLOBALES

        //checkpoints
        TgcBoundingBox[] checkpoints = new TgcBoundingBox[] { new TgcBoundingBox(), new TgcBoundingBox(), new TgcBoundingBox(), new TgcBoundingBox(), new TgcBoundingBox(), new TgcBoundingBox(), new TgcBoundingBox(), new TgcBoundingBox(), new TgcBoundingBox(), new TgcBoundingBox(), };
        Vector3 ultimoCheck = new Vector3(0, -100, -150);
        //Herramientas Framework
        Microsoft.DirectX.Direct3D.Device d3dDevice;
        

        //skybox
        TgcSkyBox skyBox;

        //Moto
        TgcMesh motorcycle;

        //Piramide
        TgcMesh piramid;

        //movimiento
        float tiempoAcelerando = 0f;
        float tiempoDescelerando = 0f;
        float velIni = 0f;

        //collisions
        TgcElipsoid characterElipsoid;
        List<Collider> objetosColisionables = new List<Collider>();
        ElipsoidCollisionManager collisionManager;
        bool tocandoPiso = false;
        bool saltando = false;
        Vector3 ultimaNormal = new Vector3(0, 0, 0);
        Vector3 ultimoMov = new Vector3(0, 0, 0);

        //TrabaPista
        TgcBox lineaInicio;

        //Final Pista
        TgcBox lineaFin;

        //Ciudad
        TgcScene scene;

        TgcText2d textGanaste;
        TgcText2d textGanaste2;
        TgcText2d textoContadorTiempo;
        TgcText2d textoMejorTiempo;

        //Debug
        TgcArrow collisionNormalArrow;
        TgcArrow directionArrow;
        TgcBox collisionPoint;
        bool showBB;

        //tiempo
        static float mejor_tiempo = 0;
        float tiempoTranscurrido = 0;

        bool terminoJuego = false;

        //iluminacion
        ShadowMap ojalaQueAnde;


        //sprites
       static TgcSprite mapita;
        static TgcSprite motoSprite;
       static TgcSprite timer;
        float zAnterior = -150;
      static  Size textureSize;
       static Size screenSize;
    static    Size motoTextureSize;
        static bool inicializo = false;
        static Vector2 posMotoInicial;

        static class Fuentes
        {
            //Coleccion con las fuentes
            public static System.Drawing.Text.PrivateFontCollection fuente = new System.Drawing.Text.PrivateFontCollection();

            //Carga las fuentes
            public static void cargarFuentes()
            {
                fuente.AddFontFile(GuiController.Instance.AlumnoEjemplosMediaDir + "fonts\\24.ttf");

            }
        }

        public Game()
        {
            Fuentes.cargarFuentes();

            //sprites
            if (!inicializo)
            {
                mapita = new TgcSprite();
                mapita.Texture = TgcTexture.createTexture(GuiController.Instance.AlumnoEjemplosMediaDir + "\\sprites\\preview.png");
                motoSprite = new TgcSprite();
                motoSprite.Texture = TgcTexture.createTexture(GuiController.Instance.AlumnoEjemplosMediaDir + "\\sprites\\dirtbike.png");
                timer = new TgcSprite();
                timer.Texture = TgcTexture.createTexture(GuiController.Instance.AlumnoEjemplosMediaDir + "\\sprites\\gate.png");

                //Ubicarlo centrado en la pantalla
                screenSize = GuiController.Instance.Panel3d.Size;
                textureSize = mapita.Texture.Size;
                motoTextureSize = motoSprite.Texture.Size;
                mapita.Scaling = (new Vector2(0.7f, 0.7f));
                motoSprite.Scaling = (new Vector2(0.5f, 0.5f));

                mapita.Position = new Vector2(FastMath.Max(screenSize.Width / 2 - textureSize.Width * 0.7f / 2, 0), 16);

                motoSprite.Position = new Vector2(FastMath.Max(screenSize.Width / 2 - motoTextureSize.Width * 0.5f / 2 - textureSize.Width * 0.7f / 2, 0), 8);

                timer.Position = new Vector2(screenSize.Width - 250, screenSize.Height - 125);

                timer.Scaling = new Vector2(0.9f, 0.8f);
                posMotoInicial = motoSprite.Position;
                inicializo = true;
            }
            d3dDevice = GuiController.Instance.D3dDevice;

            d3dDevice.Transform.Projection =
                Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(45.0f),
                TgcD3dDevice.aspectRatio, 1f, 5000000f);

            string texturesPath = GuiController.Instance.AlumnoEjemplosMediaDir + "skybox\\";
            TgcSceneLoader loader = new TgcSceneLoader();
            collisionManager = new ElipsoidCollisionManager();
            collisionManager.GravityEnabled = false;
            tiempoAcelerando = 0f;
            tiempoDescelerando = 0f;
            velIni = 0f;
            tocandoPiso = false;
            saltando = true;
            ultimoMov = new Vector3(0, 0, 0);

            //skybox
            inicializarSkybox(texturesPath);

            //checkpoints
            int posCP = -150;
            foreach (TgcBoundingBox bb in checkpoints)
            {
                bb.setExtremes(new Vector3(-200, -100, posCP), new Vector3(200, 1000, posCP - 10));
                posCP -= 1300;
            }

            //carga la ciudad
            scene = loader.loadSceneFromFile(GuiController.Instance.AlumnoEjemplosMediaDir + "pistaDesierto\\pistaDesierto2-TgcScene.xml");

            //cargo la moto
            motorcycle = loader.loadSceneFromFile(GuiController.Instance.AlumnoEjemplosMediaDir + "moto\\Moto2-TgcScene.xml").Meshes[0];
            motorcycle.move(-40, 100, -150);

            //cargo la piramide
            piramid = loader.loadSceneFromFile(GuiController.Instance.AlumnoEjemplosMediaDir + "piramide\\piramide-TgcScene.xml").Meshes[0];
            piramid.Scale = new Vector3(5, 5, 5);
            piramid.move(-265, -45, -13750);

            //cargo texto ganaste

            textGanaste = new TgcText2d();
            textGanaste2 = new TgcText2d();

            //Cargar Textos
            textGanaste.Text = "FELICIDADES, HAS GANADO";
            textGanaste2.Text = "APRETE Q PARA VOLVER AL MENU";
            textGanaste.Position = new Point(0, 50);
            textGanaste2.Position = new Point(0, 100);
            textGanaste.changeFont(new System.Drawing.Font("TimesNewRoman", 23, FontStyle.Bold | FontStyle.Bold));
            textGanaste2.changeFont(new System.Drawing.Font("TimesNewRoman", 23, FontStyle.Bold | FontStyle.Bold));

            textGanaste.Color = Color.White;
            textGanaste2.Color = Color.White;

            // Creo texto contador de tiempo
            textoContadorTiempo = new TgcText2d();
            textoContadorTiempo.Color = Color.Black;
            textoContadorTiempo.Align = TgcText2d.TextAlign.RIGHT;
            textoContadorTiempo.Position = new Point(630, 400); //(680, 400)
            textoContadorTiempo.Size = new Size(300, 100);
            textoContadorTiempo.changeFont(new System.Drawing.Font(Fuentes.fuente.Families[0], 25, FontStyle.Regular));

            // Creo texto mejor tiempo
            textoMejorTiempo = new TgcText2d();
            textoMejorTiempo.Color = Color.Black;
            textoMejorTiempo.Align = TgcText2d.TextAlign.RIGHT;
            textoMejorTiempo.Position = new Point(630, 430);  //(680, 430)
            textoMejorTiempo.Size = new Size(300, 100);
            textoMejorTiempo.changeFont(new System.Drawing.Font(Fuentes.fuente.Families[0], 25, FontStyle.Regular));
            

            //camara
            GuiController.Instance.ThirdPersonCamera.Enable = true;
            GuiController.Instance.ThirdPersonCamera.setCamera(motorcycle.Position + new Vector3(0,0,-100), 10, 200);
            GuiController.Instance.ThirdPersonCamera.rotateY(-0.7f);

            //creo la bounding elipsoid

            motorcycle.Scale = new Vector3(0.5f, 0.5f, 0.5f);

            // motorcycle.AutoUpdateBoundingBox = false;
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

            //agrego Piramide Como objeto Colisionable
            objetosColisionables.Add(BoundingBoxCollider.fromBoundingBox(piramid.BoundingBox));

            //Cargo lineaInicio

            lineaInicio = new TgcBox();
            lineaInicio.Position = new Vector3(-7, 30, -50);
            lineaInicio.Size = new Vector3(200, 2000, 1);
            lineaInicio.updateValues();

            //Cargo lineaFin
            lineaFin = new TgcBox();
            lineaFin.Size = new Vector3(100, 1000, 1);
            lineaFin.Position = new Vector3(0, 15, -13250);
            lineaFin.Color = Color.White;
            lineaFin.updateValues();

            //La agrego como objeto colisionable
            objetosColisionables.Add(BoundingBoxCollider.fromBoundingBox(lineaInicio.BoundingBox));

            showBB = false;


            //iluminacion
            ojalaQueAnde = new ShadowMap(scene, motorcycle);
        

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
            skyBox.Size = new Vector3(10000, 15000, 25000);
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "top.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "down.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "right.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "left.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "back.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "front.jpg");
            skyBox.SkyEpsilon = (0.5f);
            skyBox.updateValues();
        }

        public float anguloEntreVectores(Vector3 v1, Vector3 v2)
        {
            var producto = Vector3.Dot(v1, v2);
            var prodModulos = v1.Length() * v2.Length();
            return (FastMath.Acos(producto / prodModulos));
        }

        public bool pasoPorAlgunCheck()
        {
            foreach (TgcBoundingBox cp in checkpoints)
            {
                if (TgcCollisionUtils.testAABBAABB(motorcycle.BoundingBox, cp))
                {
                    return true;
                }
            }
            return false;
        }

        public string FormatearTiempo(float tiempo)
        {
            int minutos;
            int segundos;
            int centesimas;

            minutos = (int)(tiempo / 60);
            segundos = (int)(tiempo - minutos * 60);
            centesimas = (int)((tiempo - segundos - minutos * 60) * 100);

            return minutos.ToString().PadLeft(2, '0') + ":" + segundos.ToString().PadLeft(2, '0') + ":" + centesimas.ToString().PadLeft(2, '0');
        }

        public void activar(ref AlumnoEjemplos.MiGrupo.EjemploAlumno.states estado, float elapsedTime)
        {


            TgcD3dInput d3dInput = GuiController.Instance.D3dInput;

            var original_pos = motorcycle.Position;
            var original_rot = motorcycle.Rotation;

            float bigElapsed = elapsedTime * 20 + 0.1f;
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
            
            if (d3dInput.keyPressed(Key.B))
            {
                showBB = !showBB;
            }


            if (d3dInput.keyPressed(Key.C))
            {
                motorcycle.Position = ultimoCheck + new Vector3(0, 10, 0);
                characterElipsoid.setCenter(ultimoCheck + new Vector3(0, 10, 0));
                tiempoAcelerando = 0f;
                tiempoDescelerando = 0f;
                velIni = 0f;
                motorcycle.Rotation = new Vector3(0, 0, 0);
            }


            if (d3dInput.keyPressed(Key.P))
            {
                motorcycle.move(new Vector3(0, 5, 0));
                characterElipsoid.moveCenter(new Vector3(0, 5, 0));
                tiempoAcelerando = 1f;
                tiempoDescelerando = 1f;
            }

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
            Vector3 realMovement;
            Vector3 movementVector;

            if (!collisionManager.isOnTheGround() && !rotating)
            {
                if (motorcycle.Rotation.X < -0.1)
                {
                    motorcycle.rotateX(1f * elapsedTime);
                }
                if (motorcycle.Rotation.X > 0.1)
                {
                    motorcycle.rotateX(-1f * elapsedTime);
                }
            }

            //CALCULO VECTOR MOVIMIENTO
            movementVector = Vector3.Empty;
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
            realMovement = movementVector;
            if (moving && collisionManager.isOnTheGround())
            {
                var asd = anguloEntreVectores(ultimaNormal, new Vector3(0, 1, 0));
                if (anguloEntreVectores(ultimaNormal, new Vector3(0, 0, 1)) > 1.5708f)
                {
                    asd = -asd;
                }

                motorcycle.Rotation = new Vector3(asd, 0, 0);

            }



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
                GuiController.Instance.UserVars.setValue("Rotation", TgcParserUtils.printVector3(motorcycle.Rotation));


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

            //guardo el checkpoint
            if (pasoPorAlgunCheck())
            {
                ultimoCheck = motorcycle.Position;
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
            GuiController.Instance.ThirdPersonCamera.Target = motorcycle.Position + new Vector3(0,0,-100);

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

            if (collisionManager.isOnTheGround())
            {
                saltando = false;
            }
            else { saltando = true; }

            if (TgcCollisionUtils.testAABBAABB(motorcycle.BoundingBox, lineaFin.BoundingBox))
            {
                terminoJuego = true;
                objetosColisionables.Clear();
            }

            if (terminoJuego)
            {
                GuiController.Instance.ThirdPersonCamera.Target = lineaFin.Position;
               
                if(mejor_tiempo == 0)
                {
                    mejor_tiempo = tiempoTranscurrido;
                }
                mejor_tiempo = Math.Min(mejor_tiempo, tiempoTranscurrido);
                motorcycle.move(0, 100, -150);
               
            }

            ultimaNormal = collisionManager.Result.collisionNormal;

            //TERMINA DEBUG

            //     if (anguloEntreVectores(original_rot, motorcycle.Rotation) > 0.45f || anguloEntreVectores(original_rot, motorcycle.Rotation) < -0.45f)
            //     {
            //         tocandoPiso = false;
            //         motorcycle.Rotation = original_rot;
            //         motorcycle.Position = original_pos;
            //         terminoDeSaltar = false;
            //     }

            //Renders
            if (!terminoJuego)
            {
                tiempoTranscurrido += elapsedTime;
            }
            
            textoContadorTiempo.Text = "Tiempo: " + FormatearTiempo(tiempoTranscurrido);
            textoMejorTiempo.Text = "Record: " + FormatearTiempo(mejor_tiempo);



          
            ojalaQueAnde.activar(elapsedTime,textGanaste,textGanaste2,textoContadorTiempo,textoMejorTiempo,piramid,skyBox,terminoJuego,timer);
      //      motorcycle.render();
           // scene.renderAll();

            if (showBB)
            {
                collisionNormalArrow.render();

                collisionPoint.render();

                directionArrow.render();
                characterElipsoid.render();
                foreach (TgcBoundingBox bb in checkpoints)
                {
                    bb.render();
                }
            }


            motoSprite.Position = motoSprite.Position + new Vector2(-(motorcycle.Position.Z - zAnterior)*textureSize.Width*0.7f/13200, 0);
            

          

            //Iniciar dibujado de todos los Sprites de la escena (en este caso es solo uno)
            GuiController.Instance.Drawer2D.beginDrawSprite();
            //Dibujar sprite (si hubiese mas, deberian ir todos aquí)
         
            mapita.render();
            motoSprite.render();
            

            //Finalizar el dibujado de Sprites
            GuiController.Instance.Drawer2D.endDrawSprite();

            zAnterior = motorcycle.Position.Z;

        }

        public void close()
        {
            //motorcycle.dispose();
            //scene.disposeAll();
            motoSprite.Position = posMotoInicial;
            ojalaQueAnde.close();
            skyBox.dispose();
            characterElipsoid.dispose();
            collisionNormalArrow.dispose();
            directionArrow.dispose();
            piramid.dispose();
            GuiController.Instance.ThirdPersonCamera.rotateY(0.7f);

        }

    }
}
