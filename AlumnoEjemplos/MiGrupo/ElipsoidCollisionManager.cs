using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Utils.TgcGeometry;
using Microsoft.DirectX;
using TgcViewer;
using TgcViewer.Utils.Collision.ElipsoidCollision;

namespace AlumnoEjemplos.MiGrupo
{
    /// <summary>
    /// Herramienta para realizar el movimiento de un Elipsoide con detección de colisiones,
    /// efecto de Sliding y gravedad.
    /// Basado en el paper de Kasper Fauerby
    /// http://www.peroxide.dk/papers/collision/collision.pdf
    /// Su utiliza una estrategia distinta al paper en el nivel más bajo de colisión.
    /// Se utilizan distintos tipos de objetos Collider: a nivel de triangulos y a nivel de BoundingBox.
    /// Cada uno posee su propio algoritmo de colision optimizado para el caso.
    /// </summary>
    public class ElipsoidCollisionManager
    {
        const float EPSILON = 0.05f;
        

        private Vector3 gravityForce;
        /// <summary>
        /// Vector que representa la fuerza de gravedad.
        /// Debe tener un valor negativo en Y para que la fuerza atraiga hacia el suelo
        /// </summary>
        public Vector3 GravityForce
        {
            get { return gravityForce; }
            set { gravityForce = value; }
        }

        private bool gravityEnabled;
        /// <summary>
        /// Habilita o deshabilita la aplicación de fuerza de gravedad
        /// </summary>
        public bool GravityEnabled
        {
            get { return gravityEnabled; }
            set { gravityEnabled = value; }
        }

        private float slideFactor;
        /// <summary>
        /// Multiplicador de la fuerza de Sliding
        /// </summary>
        public float SlideFactor
        {
            get { return slideFactor; }
            set { slideFactor = value; }
        }


        private float onGroundMinDotValue;
        /// <summary>
        /// Valor que indica la maxima pendiente que se puede trepar sin empezar
        /// a sufrir los efectos de gravedad. Valor entre [0, 1] siendo 0 que puede
        /// trepar todo y 1 que no puede trepar nada.
        /// El valor Y de la normal de la superficie contra la que se colisiona tiene
        /// que ser superior a este parametro para permitir trepar la pendiente.
        /// </summary>
        public float OnGroundMinDotValue
        {
            get { return onGroundMinDotValue; }
            set { onGroundMinDotValue = value; }
        }

        CollisionResult result;
        /// <summary>
        /// Resultado de colision
        /// </summary>
        public CollisionResult Result
        {
            get { return result; }
        }
        

        List<Collider> objetosCandidatos;
        TgcBoundingSphere movementSphere;
        TgcBoundingSphere eSphere;
       

        /// <summary>
        /// Crear inicializado
        /// </summary>
        public ElipsoidCollisionManager()
        {
            gravityEnabled = true;
            gravityForce = new Vector3(0, -10, 0);
            slideFactor = 1.3f;
            movementSphere = new TgcBoundingSphere();
            eSphere = new TgcBoundingSphere();
            objetosCandidatos = new List<Collider>();
            onGroundMinDotValue = 0.72f;

            result = new CollisionResult();
            result.collisionFound = false;
            result.collisionNormal = Vector3.Empty;
            result.collisionPoint = Vector3.Empty;
            result.realMovmentVector = Vector3.Empty;
        }

        /// <summary>
        /// Mover Elipsoide con detección de colisiones, sliding y gravedad.
        /// Se actualiza la posición del centro del Elipsoide
        /// </summary>
        /// <param name="characterElipsoid">Elipsoide del cuerpo a mover</param>
        /// <param name="movementVector">Movimiento a realizar</param>
        /// <param name="colliders">Obstáculos contra los cuales se puede colisionar</param>
        /// <returns>Desplazamiento relativo final efecutado al Elipsoide</returns> 
        public Vector3 moveCharacter(TgcElipsoid characterElipsoid, Vector3 movementVector, List<Collider> colliders)
        {
            //Guardar posicion original del Elipsoide
            Vector3 originalElipsoidCenter = characterElipsoid.Center;

            //Pasar elipsoid space
            Vector3 eCenter = TgcVectorUtils.div(characterElipsoid.Center, characterElipsoid.Radius);
            Vector3 eMovementVector = TgcVectorUtils.div(movementVector, characterElipsoid.Radius);
            eSphere.setValues(eCenter, 1);
            Vector3 eOrigCenter = eSphere.Center;


            //Ver si la distancia a recorrer es para tener en cuenta
            float distanceToTravelSq = movementVector.LengthSq();
            if (distanceToTravelSq >= EPSILON)
            {
                //Mover la distancia pedida
                selectPotentialColliders(characterElipsoid, movementVector, colliders);
                this.result = doCollideWithWorld(eSphere, eMovementVector, characterElipsoid.Radius, objetosCandidatos, 0, movementSphere, 1);
            }

            //Aplicar gravedad
            if (gravityEnabled)
            {
                //Mover con gravedad
                Vector3 eGravity = TgcVectorUtils.div(gravityForce, characterElipsoid.Radius);
                selectPotentialColliders(characterElipsoid, eGravity, colliders);
                this.result = doCollideWithWorld(eSphere, eGravity, characterElipsoid.Radius, objetosCandidatos, 0, movementSphere, onGroundMinDotValue);
            }

            //Mover Elipsoid pasando valores de colision a R3
            Vector3 movement = TgcVectorUtils.mul(eSphere.Center - eOrigCenter, characterElipsoid.Radius);
            characterElipsoid.moveCenter(movement);

            //Ajustar resultados
            result.realMovmentVector = TgcVectorUtils.mul(result.realMovmentVector, characterElipsoid.Radius);
            result.collisionPoint = TgcVectorUtils.mul(result.collisionPoint, characterElipsoid.Radius);


            return movement;
        }


        /// <summary>
        /// Selecciona todos los colliders que estan dentro de la esfera que representa el movimiento.
        /// Carga la lista objetosCandidatos
        /// </summary>
        private void selectPotentialColliders(TgcElipsoid characterElipsoid, Vector3 movementVector, List<Collider> colliders)
        {
            //Dejar solo los obstáculos que están dentro del radio de movimiento del elipsoide (lo consideramos una esfera, con su mayor radio)
            Vector3 halfMovementVec = Vector3.Multiply(movementVector, 0.5f);
            movementSphere.setValues(
                characterElipsoid.Center + halfMovementVec,
                halfMovementVec.Length() + characterElipsoid.getMaxRadius()
                );
            

            //Elegir todos los colliders que pasan un test Sphere-Sphere
            objetosCandidatos.Clear();
            foreach (Collider collider in colliders)
            {
                if (collider.Enable && TgcCollisionUtils.testSphereSphere(movementSphere, collider.BoundingSphere))
                {
                    objetosCandidatos.Add(collider);
                }
            }
        }

        /// <summary>
        /// Resultado de colision
        /// </summary>
        public struct CollisionResult
        {
            /// <summary>
            /// True si hubo colision
            /// </summary>
            public bool collisionFound;

            /// <summary>
            /// Movimiento realmente aplicado
            /// </summary>
            public Vector3 realMovmentVector;

            /// <summary>
            /// Punto de colision del Elipsoide contre el Collider
            /// </summary>
            public Vector3 collisionPoint;

            /// <summary>
            /// Normal de la superficie del Collider contra la cual se colisiono
            /// </summary>
            public Vector3 collisionNormal;

            /// <summary>
            /// Objeto contra el cual se colisiono
            /// </summary>
            public Collider collider;
        }


        /// <summary>
        /// Detección de colisiones recursiva
        /// </summary>
        /// <param name="eSphere">Sphere de radio 1 pasada a Elipsoid space</param>
        /// <param name="eMovementVector">Movimiento pasado a Elipsoid space</param>
        /// <param name="eRadius">Radio de la elipsoide</param>
        /// <param name="colliders">Objetos contra los cuales colisionar</param>
        /// <param name="recursionDepth">Nivel de recursividad</param>
        /// <param name="movementSphere">Esfera real que representa el movimiento abarcado</param>
        /// <param name="slidingMinY">Minimo valor de normal Y de colision para hacer sliding</param>
        /// <returns>Resultado de colision</returns>
        public CollisionResult doCollideWithWorld(TgcBoundingSphere eSphere, Vector3 eMovementVector, Vector3 eRadius, List<Collider> colliders, int recursionDepth, TgcBoundingSphere movementSphere, float slidingMinY)
        {
            CollisionResult result = new CollisionResult();
            result.collisionFound = false;

            //Limitar recursividad
            if (recursionDepth > 5)
            {
                return result;
            }

            //Posicion deseada
            Vector3 nextSphereCenter = eSphere.Center + eMovementVector;

            //Buscar el punto de colision mas cercano de todos los objetos candidatos
            Vector3 q;
            float t;
            Vector3 n;
            float minT = float.MaxValue;
            foreach (Collider collider in colliders)
            {
                //Colisionar Sphere en movimiento contra Collider (cada Collider resuelve la colision)
                if (collider.intersectMovingElipsoid(eSphere, eMovementVector, eRadius, movementSphere, out t, out q, out n))
                {
                    //Quedarse con el menor instante de colision
                    if(t < minT)
                    {
                        minT = t;
                        result.collisionFound = true;
                        result.collisionPoint = q;
                        result.collisionNormal = n;
                        result.collider = collider;
                    }
                }
            }

            //Si nunca hubo colisión, avanzar todo lo requerido
            if (!result.collisionFound)
            {
                //Avanzar todo lo pedido
                eSphere.moveCenter(eMovementVector);
                result.realMovmentVector = eMovementVector;
                result.collisionNormal = Vector3.Empty;
                result.collisionPoint = Vector3.Empty;
                result.collider = null;
                return result;
            }


            //Solo movernos si ya no estamos muy cerca
            if (minT >= EPSILON)
            {
                //Restar un poco al instante de colision, para movernos hasta casi esa distancia
                minT -= EPSILON;
                result.realMovmentVector = eMovementVector * minT;
                eSphere.moveCenter(result.realMovmentVector);

                //Quitarle al punto de colision el EPSILON restado al movimiento, para no afectar al plano de sliding
                Vector3 v = Vector3.Normalize(result.realMovmentVector);
                result.collisionPoint -= v * EPSILON;
            }


            //Calcular plano de Sliding, como un plano tangete al punto de colision con la esfera, apuntando hacia el centro de la esfera
            Vector3 slidePlaneOrigin = result.collisionPoint;
            Vector3 slidePlaneNormal = eSphere.Center - result.collisionPoint;
            slidePlaneNormal.Normalize();
            Plane slidePlane = Plane.FromPointNormal(slidePlaneOrigin, slidePlaneNormal);

            //Calcular vector de movimiento para sliding, proyectando el punto de destino original sobre el plano de sliding
            float distance = TgcCollisionUtils.distPointPlane(nextSphereCenter, slidePlane);
            Vector3 newDestinationPoint = nextSphereCenter - distance * slidePlaneNormal;
            Vector3 slideMovementVector = newDestinationPoint - result.collisionPoint;

            //No hacer recursividad si es muy pequeño
            slideMovementVector.Scale(slideFactor);
            if (slideMovementVector.Length() < EPSILON)
            {
                return result;
            }

            //Ver si posee la suficiente pendiente en Y para hacer sliding
            if (result.collisionNormal.Y <= slidingMinY)
            {
                //Recursividad para aplicar sliding
                doCollideWithWorld(eSphere, slideMovementVector, eRadius, colliders, recursionDepth + 1, movementSphere, slidingMinY);
            }

            
            return result;
        }

        
        /// <summary>
        /// Indica si el objeto se encuentra con los pies sobre alguna superficie, sino significa
        /// que está cayendo o saltando.
        /// </summary>
        /// <returns>True si el objeto se encuentra parado sobre una superficie</returns>
        public bool isOnTheGround()
        {
            if(result.collisionNormal == Vector3.Empty)
                return false;

            //return true;
            //return lastCollisionNormal.Y >= onGroundMinDotValue;
            return result.collisionNormal.Y >= 0;
        }
        

    }
}
