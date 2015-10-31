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
