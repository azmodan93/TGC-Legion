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
    public class MenuInicio
    {
        TgcSprite sprite;
        TgcD3dInput input;
        TgcText2d[] menuLineas;
        int inicialX = -(GuiController.Instance.Panel3d.Width / 2) + 100;
        int inicialY = (GuiController.Instance.Panel3d.Height / 2);
        int distEntreLineas = 80;
        int width = GuiController.Instance.Panel3d.Width;
        int height = GuiController.Instance.Panel3d.Height;
        int largoDeLinea = 150;
        int altoDeLinea = 38;

        public MenuInicio()
        {

            //Crear Sprite
            sprite = new TgcSprite();
            sprite.Texture = TgcTexture.createTexture(GuiController.Instance.AlumnoEjemplosMediaDir + "menu\\inicio.jpg");

            //Ubicarlo centrado en la pantalla
            Size screenSize = GuiController.Instance.Panel3d.Size;
            Size textureSize = sprite.Texture.Size;
            sprite.Position = new Vector2(0, 0);
            sprite.Scaling = new Vector2((float)screenSize.Width / textureSize.Width, (float)screenSize.Height / textureSize.Height + 0.01f);

            //Crear Text
            menuLineas = new TgcText2d[] { new TgcText2d(), new TgcText2d(), new TgcText2d(), new TgcText2d() };

            //Cargar Textos
            menuLineas[0].Text = "(I) Iniciar";
            menuLineas[0].Position = new Point(inicialX, inicialY - distEntreLineas);
            menuLineas[0].changeFont(new System.Drawing.Font("TimesNewRoman", 23, FontStyle.Bold | FontStyle.Bold));

            menuLineas[1].Text = "(C) Creditos";
            menuLineas[1].Position = new Point(inicialX, inicialY);
            menuLineas[1].changeFont(new System.Drawing.Font("TimesNewRoman", 23, FontStyle.Bold | FontStyle.Bold));

            menuLineas[2].Text = "(A) Ayuda";
            menuLineas[2].Position = new Point(inicialX, inicialY + distEntreLineas);
            menuLineas[2].changeFont(new System.Drawing.Font("TimesNewRoman", 23, FontStyle.Bold | FontStyle.Bold));


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

            if (input.keyPressed(Key.C))
            {

                estado = EjemploAlumno.states.creditos;

            }

            if (input.keyPressed(Key.A))
            {

                estado = EjemploAlumno.states.ayuda;

            }

            if (input.keyPressed(Key.I))
            {

                estado = EjemploAlumno.states.game;

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
