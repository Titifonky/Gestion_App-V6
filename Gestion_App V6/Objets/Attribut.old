using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gestion
{
    public enum TypeSQL_e
    {
        cAuto = 0,
        cInt = 1,
        cBool = 2,
        cDbl = 3,
        cDate = 4,
        cString = 5,
        cEnum = 6,
        cSerial
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Propriete : Attribute
    {
        protected TypeSQL_e _Type = TypeSQL_e.cAuto;
        protected String _Contrainte = "";

        public Propriete() { }

        public TypeSQL_e Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        public String Contrainte
        {
            get { return _Contrainte; }
            set { _Contrainte = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Cle : Propriete
    {
        public Cle()
        {
            _Type = TypeSQL_e.cInt;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ClePrimaire : Cle
    {
        public ClePrimaire()
        {
            _Contrainte = "NOT NULL AUTO_INCREMENT";
            _Type = TypeSQL_e.cSerial;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CleEtrangere : Cle
    {
        public CleEtrangere()
        {
            _Contrainte = "NOT NULL";
            _Type = TypeSQL_e.cInt;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Max : Attribute
    {
        String _NomPropriete = "";

        public Max() { }

        public String NomPropriete
        {
            get { return _NomPropriete; }
            set { _NomPropriete = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Tri : Attribute
    {
        int _No = 0;
        Boolean _Modifiable = false;
        ListSortDirection _DirectionTri = ListSortDirection.Ascending;

        public Tri() { }

        public int No
        {
            get { return _No; }
            set { _No = value; }
        }

        public ListSortDirection DirectionTri
        {
            get { return _DirectionTri; }
            set { _DirectionTri = value; }
        }

        public Boolean Modifiable
        {
            get { return _Modifiable; }
            set { _Modifiable = value; }
        }
    }
    
    [AttributeUsage(AttributeTargets.Property)]
    public class NePasCopier : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class ForcerCopie : Attribute { }

    /// <summary>
    /// Force l'insertion de l'objet dans la base de donnée à la création
    /// pour avoir un id
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ForcerAjout : Attribute { }

    public class UniteAttribute : Attribute
    {
        String _Unite = "";

        public UniteAttribute() { }

        public UniteAttribute(String unite)
        {
            _Unite = unite;
        }

        public String Unite
        {
            get { return _Unite; }
            set { _Unite = value; }
        }
    }
}
