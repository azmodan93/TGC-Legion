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
using TgcViewer.Utils._2D;

namespace AlumnoEjemplos.MiGrupo
{
    /// <summary>
    /// Ejemplo del alumno
    /// </summary>
    public class EjemploAlumno : TgcExample
    {
        //VARIABLES GLOBALES

        //Para la pantalla de inicio
        MenuInicio menuInicio;
        MenuCreditos menuCreditos;
        MenuAyuda menuAyuda;
        Game game;

        bool estaCorriendoGame = false;

        states estado;

        public enum states
        {
            inicio,
            creditos,
            ayuda,
            game,
        }

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
            return "Juego de moto en 2D - Los comandos de juego están descriptos en la opcion Ayuda del menú principal";
        }

        /// <summary>
        /// Método que se llama una sola vez,  al principio cuando se ejecuta el ejemplo.
        /// Escribir aquí todo el código de inicialización: cargar modelos, texturas, modifiers, uservars, etc.
        /// Borrar todo lo que no haga falta
        /// </summary>
        /// 

        public override void init()
        {

            estado = states.inicio;

            //Creo Menu Inicio
            menuInicio = new MenuInicio();
            menuCreditos = new MenuCreditos();
            menuAyuda = new MenuAyuda();
            game = new Game();

            //Modifier para ver BoundingBox
            GuiController.Instance.Modifiers.addBoolean("Collisions", "Collisions", true);

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
            GuiController.Instance.UserVars.addVar("Rotation");

            GuiController.Instance.UserVars.addVar("AnguloZ");
            GuiController.Instance.UserVars.addVar("AnguloY");

            GuiController.Instance.UserVars.addVar("moveForward");
            GuiController.Instance.UserVars.addVar("velIni");
            GuiController.Instance.UserVars.addVar("aceleracion");
            GuiController.Instance.UserVars.addVar("tiempoAcel");
            GuiController.Instance.UserVars.addVar("tiempoDesce");
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

            if (d3dInput.keyPressed(Key.Q))
            {
                estado = EjemploAlumno.states.inicio;
                game.close();
                game = new Game();
            }

            //pantalla De Inicio
            switch (estado)
            {
                case states.inicio:
                    menuInicio.activar(ref estado);
                    break;

                case states.creditos:
                    menuCreditos.activar(ref estado);
                    break;

                case states.ayuda:
                    menuAyuda.activar(ref estado);
                    break;

                case states.game:
                    estaCorriendoGame = true;
                    game.activar(ref estado, elapsedTime);
                    break;
            }
        }

        /// <summary>
        /// Método que se llama cuando termina la ejecución del ejemplo.
        /// Hacer dispose() de todos los objetos creados.
        /// </summary>
        public override void close()
        {
            if (estaCorriendoGame)
            {
                game.close();
            }
        }

    }
}
