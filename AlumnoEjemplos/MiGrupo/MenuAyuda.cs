using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using TgcViewer;
using TgcViewer.Utils._2D;
using TgcViewer.Utils.Input;
using TgcViewer.Utils.TgcSceneLoader;

namespace AlumnoEjemplos.MiGrupo
{
    public class MenuAyuda
     {
        TgcSprite sprite;
        TgcText2d[] menuLineas;
        int inicialX = -(GuiController.Instance.Panel3d.Width / 2) + 100;
        int inicialY = (GuiController.Instance.Panel3d.Height / 2);
        int distEntreLineas = 80;
        int posCreditos = GuiController.Instance.Panel3d.Height / 2;
        int width = GuiController.Instance.Panel3d.Width;
        int height = GuiController.Instance.Panel3d.Height;
        int largoDeLinea = 150;
        int altoDeLinea = 38;
        public TgcD3dInput input;

        public MenuAyuda()
        {

            //Crear Sprite
            sprite = new TgcSprite();
            sprite.Texture = TgcTexture.createTexture(GuiController.Instance.AlumnoEjemplosMediaDir + "menu\\creditos-ayuda.jpg");

            //Ubicarlo centrado en la pantalla
            Size screenSize = GuiController.Instance.Panel3d.Size;
            Size textureSize = sprite.Texture.Size;
            sprite.Position = new Vector2(0, 0);
            sprite.Scaling = new Vector2((float)screenSize.Width / textureSize.Width, (float)screenSize.Height / textureSize.Height + 0.01f);

            //Crear Text
            menuLineas = new TgcText2d[] { new TgcText2d(), new TgcText2d(), new TgcText2d(), new TgcText2d(), new TgcText2d(), new TgcText2d() };

            //Cargar Textos
            menuLineas[0].Text = "COMANDOS";
            menuLineas[0].Position = new Point(0, posCreditos - (3 * distEntreLineas));
            menuLineas[0].changeFont(new System.Drawing.Font("TimesNewRoman", 23, FontStyle.Bold | FontStyle.Bold));

            menuLineas[1].Text = "W - AVANZAR";
            menuLineas[1].Position = new Point(0, posCreditos - (2 * distEntreLineas));
            menuLineas[1].changeFont(new System.Drawing.Font("TimesNewRoman", 23, FontStyle.Bold | FontStyle.Bold));

            menuLineas[2].Text = "S - FRENAR/RETROCEDER";
            menuLineas[2].Position = new Point(0, posCreditos - distEntreLineas);
            menuLineas[2].changeFont(new System.Drawing.Font("TimesNewRoman", 23, FontStyle.Bold | FontStyle.Bold));

            menuLineas[3].Text = "A - GIRAR HACIA ATRAS";
            menuLineas[3].Position = new Point(0, posCreditos);
            menuLineas[3].changeFont(new System.Drawing.Font("TimesNewRoman", 23, FontStyle.Bold | FontStyle.Bold));

            menuLineas[4].Text = "D - GIRAR HACIA ADELANTE";
            menuLineas[4].Position = new Point(0, posCreditos + distEntreLineas);
            menuLineas[4].changeFont(new System.Drawing.Font("TimesNewRoman", 23, FontStyle.Bold | FontStyle.Bold));

            menuLineas[5].Text = "BACKSPACE - VOLVER A MENU INICIO";
            menuLineas[5].Position = new Point(0, posCreditos + (2 * distEntreLineas));
            menuLineas[5].changeFont(new System.Drawing.Font("TimesNewRoman", 23, FontStyle.Bold | FontStyle.Bold));


            //Cambio color texto
            foreach (TgcText2d linea in menuLineas)
            {
                linea.Color = Color.White;
            }

            //Inicializa el d3dInput
            input = GuiController.Instance.D3dInput;

        }

        public void activar(ref AlumnoEjemplos.MiGrupo.EjemploAlumno.states estado)
        {
            //pantalla De Inicio
            GuiController.Instance.Drawer2D.beginDrawSprite();
            sprite.render();

            //Finalizar el dibujado de Sprites
            GuiController.Instance.Drawer2D.endDrawSprite();

            //Mostrar Lineas en Menu
            foreach (TgcText2d linea in menuLineas)
            {
                linea.render();
            }

            if (input.keyPressed(Key.BackSpace))
            {

                estado = EjemploAlumno.states.inicio;

            }

        }

        public void limpiar()
        {
            sprite.dispose();

            foreach (TgcText2d linea in menuLineas)
            {
                linea.dispose();
            }

        }
    }
}
