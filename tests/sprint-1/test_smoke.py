"""Regresión de contratos HU-05/HU-06; ejecutar con unittest discover."""
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("smoke_sprint1", Path(__file__).with_name("smoke.py"))
smoke = importlib.util.module_from_spec(spec)
spec.loader.exec_module(smoke)


class DetalleAsignaturaTests(unittest.TestCase):
    def setUp(self):
        self.listado = {"id": "asignatura-sintetica", "carreraId": "carrera-sintetica",
                        "codigo": "TEST-01", "nombre": "Asignatura sintética",
                        "nivel": 1, "creditos": 3, "horas": 144}

    def test_acepta_detalle_con_unidades_vacias(self):
        smoke.comprobar_detalle_asignatura(self.listado, {**self.listado, "unidades": []})

    def test_acepta_detalle_con_unidades_registradas(self):
        smoke.comprobar_detalle_asignatura(self.listado,
            {**self.listado, "unidades": [{"id": "unidad-sintetica", "numero": 1}]})

    def test_rechaza_id_diferente(self):
        with self.assertRaises(AssertionError):
            smoke.comprobar_detalle_asignatura(self.listado, {**self.listado, "id": "otra", "unidades": []})

    def test_rechaza_cambio_en_datos_academicos(self):
        with self.assertRaises(AssertionError):
            smoke.comprobar_detalle_asignatura(self.listado, {**self.listado, "horas": 100, "unidades": []})

    def test_rechaza_campo_preexistente_ausente(self):
        detalle = {**self.listado, "unidades": []}
        del detalle["creditos"]
        with self.assertRaises(AssertionError):
            smoke.comprobar_detalle_asignatura(self.listado, detalle)

    def test_rechaza_detalle_sin_unidades(self):
        with self.assertRaises(AssertionError):
            smoke.comprobar_detalle_asignatura(self.listado, dict(self.listado))

    def test_rechaza_unidades_con_tipo_incorrecto(self):
        for unidades in [None, {}, "desconocido"]:
            with self.subTest(unidades=unidades), self.assertRaises(AssertionError):
                smoke.comprobar_detalle_asignatura(self.listado, {**self.listado, "unidades": unidades})


if __name__ == "__main__":
    unittest.main()
