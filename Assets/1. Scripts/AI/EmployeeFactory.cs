using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EmployeeCreationStatus
{
    Success,
    NoAvailablePrefab,
    MissingEmployeeComponent,
    MissingPackagingStation,
    MissingPackagingPoint
}

public readonly struct EmployeeCreationResult
{
    public EmployeeCreationResult(EmployeeCreationStatus status, Employee employee = null)
    {
        Status = status;
        Employee = employee;
    }

    public EmployeeCreationStatus Status { get; }
    public Employee Employee { get; }
    public bool Succeeded => Status == EmployeeCreationStatus.Success;
}

public sealed class EmployeeFactory
{
    private const int PackagingPointIndex = 1;

    private readonly IList<GameObject> availablePrefabs;
    private readonly IList<Employee> activeEmployees;

    public EmployeeFactory(IList<GameObject> availablePrefabs, IList<Employee> activeEmployees)
    {
        this.availablePrefabs = availablePrefabs ?? throw new ArgumentNullException(nameof(availablePrefabs));
        this.activeEmployees = activeEmployees ?? throw new ArgumentNullException(nameof(activeEmployees));
    }

    public EmployeeCreationStatus Validate(bool createsPackagingEmployee)
    {
        List<int> candidateIndices = GetCandidateIndices();
        if (candidateIndices.Count <= 0)
        {
            return GetMissingPrefabStatus();
        }

        if (!createsPackagingEmployee)
        {
            return EmployeeCreationStatus.Success;
        }

        return ResolvePackagingPoint(out _);
    }

    public EmployeeCreationResult TryCreate(bool createsPackagingEmployee)
    {
        List<int> candidateIndices = GetCandidateIndices();
        if (candidateIndices.Count <= 0)
        {
            return new EmployeeCreationResult(GetMissingPrefabStatus());
        }

        Transform packagingPoint = null;
        if (createsPackagingEmployee)
        {
            EmployeeCreationStatus packagingStatus = ResolvePackagingPoint(out packagingPoint);
            if (packagingStatus != EmployeeCreationStatus.Success)
            {
                return new EmployeeCreationResult(packagingStatus);
            }
        }

        int candidateIndex = UnityEngine.Random.Range(0, candidateIndices.Count);
        int prefabIndex = candidateIndices[candidateIndex];
        GameObject employeePrefab = availablePrefabs[prefabIndex];
        GameObject instance = UnityEngine.Object.Instantiate(employeePrefab, Vector3.zero, Quaternion.identity);
        Employee employee = instance.GetComponent<Employee>();

        if (employee == null)
        {
            UnityEngine.Object.Destroy(instance);
            return new EmployeeCreationResult(EmployeeCreationStatus.MissingEmployeeComponent);
        }

        if (createsPackagingEmployee)
        {
            NavMeshAgent agent = employee.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                UnityEngine.Object.Destroy(agent);
            }

            employee.transform.position = packagingPoint.position;
            employee.PackaingEmployee();
        }

        employee.name = employeePrefab.name;
        availablePrefabs.RemoveAt(prefabIndex);
        activeEmployees.Add(employee);

        return new EmployeeCreationResult(EmployeeCreationStatus.Success, employee);
    }

    private List<int> GetCandidateIndices()
    {
        var candidateIndices = new List<int>();

        for (int i = 0; i < availablePrefabs.Count; i++)
        {
            GameObject prefab = availablePrefabs[i];
            if (prefab != null && prefab.GetComponent<Employee>() != null)
            {
                candidateIndices.Add(i);
            }
        }

        return candidateIndices;
    }

    private EmployeeCreationStatus GetMissingPrefabStatus()
    {
        for (int i = 0; i < availablePrefabs.Count; i++)
        {
            if (availablePrefabs[i] != null)
            {
                return EmployeeCreationStatus.MissingEmployeeComponent;
            }
        }

        return EmployeeCreationStatus.NoAvailablePrefab;
    }

    private static EmployeeCreationStatus ResolvePackagingPoint(out Transform packagingPoint)
    {
        packagingPoint = null;
        BoxPackaging packaging = UnityEngine.Object.FindObjectOfType<BoxPackaging>();

        if (packaging == null)
        {
            return EmployeeCreationStatus.MissingPackagingStation;
        }

        if (packaging.transform.childCount <= PackagingPointIndex)
        {
            return EmployeeCreationStatus.MissingPackagingPoint;
        }

        packagingPoint = packaging.transform.GetChild(PackagingPointIndex);
        return EmployeeCreationStatus.Success;
    }
}
